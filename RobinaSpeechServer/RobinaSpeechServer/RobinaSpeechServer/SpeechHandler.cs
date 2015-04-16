using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.Recognition.SrgsGrammar;
using Microsoft.Speech.Synthesis;
using AustinHarris.JsonRpc;
using Microsoft.Kinect;

namespace RobinaSpeechServer
{
    
    public class SpeechHandler : JsonRpcService
    {
        public enum Microphone{Shotgun,Kinect};
        ConfigManager config = null;

        string GRAMMAR_PATH =Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)+"\\grammar.xml";

        string VOICE_NAME,HOST,SELF_ADDR;
        int PORT  , RPC_PORT;
        float DEFAULT_CONFIDENCE;
        double degree = 0;
        int count = 0;
        double degree_conf = 0;
        float confidence;

        bool speaking = false;


        int listening_id=0;
        int next_listening_id = 0;


        string sim_text;

        Microphone mic;

        void configurate()
        {
            config = new ConfigManager("speech.xml");
            VOICE_NAME = config[Scope.TTS, "voice"];
            HOST = config[Scope.Network, "HostAddress"];
            SELF_ADDR = config[Scope.Network, "SelfAddress"];
            PORT = int.Parse(config[Scope.Network, "MessagePassingPort"]);
            RPC_PORT = int.Parse(config[Scope.Network, "RPCPort"]);
            DEFAULT_CONFIDENCE = float.Parse(config[Scope.Recognition, "Confidence"]);
            
            confidence = DEFAULT_CONFIDENCE;

        }
        SrgsDocument doc;
        System.Threading.Mutex mutex;
        System.Threading.Mutex degree_mutex;
        System.Threading.Mutex tts_mutex;
        System.Threading.Mutex speaking_mutex;
        SpeechRecognitionEngine sre=null;
        System.Speech.Synthesis.SpeechSynthesizer ss = null;
        DataPublisher publisher = null;
        
        KinectAudioSource source = null;
        KinectSensor sensor;


        public SpeechHandler()
        {
            LoadDefaultConfiguration();

        }
        ~SpeechHandler()
        {
            if (sre != null)
            {
                sre.Dispose();
            }
            if (sensor != null)
            {
                sensor.Stop();
            }

        }
         
        public void init()
        {
            configurate();
            mutex = new System.Threading.Mutex();
            degree_mutex = new System.Threading.Mutex();
            tts_mutex = new System.Threading.Mutex();
            speaking_mutex = new System.Threading.Mutex();

            publisher = new DataPublisher(HOST, SELF_ADDR, PORT);
            ss = new System.Speech.Synthesis.SpeechSynthesizer();
            ss.SelectVoiceByHints(System.Speech.Synthesis.VoiceGender.Female);
           
            ss.SetOutputToDefaultAudioDevice();
            ss.SpeakCompleted += ss_SpeakCompleted;


            mic = (Microphone)Enum.Parse(typeof(Microphone), config[Scope.Recognition, "Sensor"]);
            bool kinect = false;
            do
            {
                switch (mic)
                {
                    case Microphone.Kinect:
                        try
                        {
                            if (KinectSensor.KinectSensors.Count != 0)
                            {
                                if (configKinect())
                                {
                                    sre = new SpeechRecognitionEngine(GetSpeechRecognizer(true));


                                    sre.SetInputToAudioStream(source.Start(),
                                  new Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo(
                                      Microsoft.Speech.AudioFormat.EncodingFormat.Pcm, 16000, 16, 1,
                                      32000, 2, null));
                                    kinect = true;
                                    Console.WriteLine("Kinect is active");
                                }
                            }
                            break;
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Error on kinect");
                            //mic = Microphone.Shotgun;

                            continue;
                        }
                    case Microphone.Shotgun:
                        try
                        {
                            if (!kinect)
                            {
                                Console.WriteLine("Microphone is active");
                                sre = new SpeechRecognitionEngine(GetSpeechRecognizer(false));
                                sre.SetInputToDefaultAudioDevice();
                               
                            }
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("No Microphone");
                            //mic = Microphone.Kinect;
                            continue;
                        }
                        break;
                }



            sre.EmulateRecognizeCompleted += sre_EmulateRecognizeCompleted;

            sre.LoadGrammarCompleted+=sre_LoadGrammarCompleted;
            sre.SpeechDetected+=sre_SpeechDetected;
            sre.SpeechHypothesized+=sre_SpeechHypothesized;
            sre.SpeechRecognitionRejected+=sre_SpeechRecognitionRejected;
            sre.SpeechRecognized +=sre_SpeechRecognized;
            sre.RecognizeCompleted += sre_RecognizeCompleted;
            try
            {
                doc = new SrgsDocument(config[Scope.Grammar,"path"]);


                Grammar g = new Grammar(doc, config[Scope.Grammar, "default"]);

                //Grammar g = new Grammar(GRAMMAR_PATH, "default");

                sre.LoadGrammar(g);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in parsing Grammar");
                Console.WriteLine(ex.Message);
                System.Threading.Thread.Sleep(1000);
                continue;
            }
            foreach (Grammar a in sre.Grammars)
            {
                Console.WriteLine(a.RuleName);
            }
            //sre.UpdateRecognizerSetting("ResourceUsage", 90);
            try
            {

                sre.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Microhpone is down");
                continue;
            }
                
            break;
            } while (true);
            Console.WriteLine("Started!!");
        }

        
        

        void ss_SpeakCompleted(object sender, System.Speech.Synthesis.SpeakCompletedEventArgs e)
        {
            tts_mutex.WaitOne();
            if(e.Error!=null)
            Console.WriteLine(e.Error.ToString());
            publisher.publish(new Messages.SpeakCompletedMessage(e.Prompt.IsCompleted));
            tts_mutex.ReleaseMutex();

            System.Threading.Thread.Sleep(500);
            speaking_mutex.WaitOne();
            speaking = false;
            speaking_mutex.ReleaseMutex();
            
        }


        private void LoadDefaultConfiguration()
        {
        }


        void rpcHandler(IAsyncResult res)
        {
            JsonRpcStateAsync state = (JsonRpcStateAsync)res;
            System.IO.Stream stream = state.AsyncState as System.IO.Stream;
            Console.WriteLine(state.Result);
            //System.Diagnostics.Debug.WriteLine(state.Result);
            byte[] buffer = Encoding.ASCII.GetBytes(state.Result);
            stream.Write(buffer, 0, buffer.Length);
            stream.Close();
        }

        public void Start()
        {
            System.Net.HttpListener listener = new System.Net.HttpListener();
            listener.Prefixes.Add("http://*:" + RPC_PORT.ToString()+ "/");
            listener.Start();

            var rpcResultHandler = new AsyncCallback(rpcHandler);
            while (true)
            {
                Console.WriteLine("Listening!");
                System.Net.HttpListenerContext c = listener.GetContext();
                System.Net.HttpListenerRequest req = c.Request;
                
                System.IO.MemoryStream buf = new System.IO.MemoryStream();

                req.InputStream.CopyTo((System.IO.Stream)buf);


                string command = Encoding.ASCII.GetString(buf.ToArray());
                System.Diagnostics.Debug.WriteLine(command);
                JsonRpcStateAsync async = new JsonRpcStateAsync(rpcResultHandler, c.Response.OutputStream);

                async.JsonRpc = command;
                //System.Diagnostics.Debug.WriteLine(command);
                Console.WriteLine(command);
                try
                {
                    JsonRpcProcessor.Process(async);
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Wrong number of arguments");
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        //#/////////////////////////////RPC Functions///////////////////////////////
        [AustinHarris.JsonRpc.JsonRpcMethod("Say")]
        public bool Say(string text)
        {
            Console.WriteLine("Say");
                ss.SpeakAsync(text);
                speaking_mutex.WaitOne();
                speaking = true;
                speaking_mutex.ReleaseMutex();
                return true;
        }
        [AustinHarris.JsonRpc.JsonRpcMethod("LoadGrammar")]
        public bool LoadGrammar(string name)
        {
            mutex.WaitOne();
            Console.WriteLine("Load");
            if (doc.Rules.Contains(name))
            {
                sre.LoadGrammar(new Grammar(doc, name));
                mutex.ReleaseMutex();
                return true;
            }
            mutex.ReleaseMutex();
            return false;
        }

        [AustinHarris.JsonRpc.JsonRpcMethod("UnloadGrammar")]
        public bool UnloadGrammar(string name)
        {
            mutex.WaitOne();
            try
            {
                if (!doc.Rules.Contains(name))
                {
                    if (!sre.Grammars.Contains(new Grammar(doc, name)))
                    {
                        mutex.ReleaseMutex();
                        return false;
                    }
                }
                sre.UnloadGrammar(new Grammar(doc, name));
                mutex.ReleaseMutex();
                return true;
            }
            catch (Exception)
            {
                mutex.ReleaseMutex();
                return false;
            }
            
            
        }

        [AustinHarris.JsonRpc.JsonRpcMethod("UnloadAllGrammar")]
        public bool UnLoadAllGrammars()
        {
            mutex.WaitOne();
            sre.UnloadAllGrammars();
            sre.LoadGrammar(new Grammar(doc,"default"));
            mutex.ReleaseMutex();
            return true;
        }

        [AustinHarris.JsonRpc.JsonRpcMethod("StartRecognize")]
        public bool StartRecognize(int id)
        {
            mutex.WaitOne();
            next_listening_id = id;
            mutex.ReleaseMutex();
            return true;
        }

        [AustinHarris.JsonRpc.JsonRpcMethod("EmulateRecognize")]
        public bool EmulateRecognize(string command_text)
        {
            mutex.WaitOne();
            try
            {
                sim_text = command_text;
                sre.RecognizeAsyncCancel();
                //sre.RecognizeAsyncStop();
                
                //sre.RecognizeAsync(RecogizeMode.Multiple);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return false; }
            finally { mutex.ReleaseMutex(); }
            return true;
        }

        [AustinHarris.JsonRpc.JsonRpcMethod("Configure")]
        public bool Configure(string option,string val)
        {
            mutex.WaitOne();
            switch (option)
            {
                case "voice":
                  ss.SelectVoice(val);
                    break;
                case "mic":
                    mic=(Microphone)Enum.Parse(typeof(Microphone), val);
                    goto case "reset";
                    
                case "reset":
                    init();
                    break;

            }
            mutex.ReleaseMutex();
            return true;

        }
        //#////////////////////////////////SpeechEvents///////////////////////////////
        void sre_EmulateRecognizeCompleted(object sender, EmulateRecognizeCompletedEventArgs e)
        {
            Console.WriteLine("DONE");
            Console.WriteLine(e.Cancelled);
            sre.RecognizeAsync(RecognizeMode.Multiple);
        }
        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            
            float deg = 0;
            float deg_conf = 0;
            if (source!=null)
            {
                    degree_mutex.WaitOne();
                    deg = (float)degree / count;
                    deg_conf = (float)degree_conf / count;
                    degree_mutex.ReleaseMutex();   
            }
            mutex.WaitOne();
            
            Console.WriteLine("REC: "+e.Result.Text+" "+deg.ToString());
            speaking_mutex.WaitOne();
            if(!speaking)publisher.publish(new Messages.SpeechRecognizedMessage(e,(float)deg,deg_conf,listening_id));
            speaking_mutex.ReleaseMutex();
            mutex.ReleaseMutex();
        }

        void sre_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Cancelled)
                sre.RecognizeAsyncStop();

            sre.EmulateRecognizeAsync(sim_text, EmulateOptions.Emulation);
        }

        void sre_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            degree_mutex.WaitOne();
            publisher.publish(new Messages.SpeechRejectedMessage(e,(float)degree));
            degree_mutex.ReleaseMutex();
        }

        void sre_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Console.WriteLine("HYP");
            if (source != null)
            {
                degree_mutex.WaitOne();
                count++;
                degree += source.SoundSourceAngle;
                degree_conf += source.SoundSourceAngleConfidence;
                degree_mutex.ReleaseMutex();
            }
 


            degree_mutex.WaitOne();
            Console.WriteLine(e.Result.Text);
            publisher.publish(new Messages.SpeechHypothesizedMessage(e,(float)degree));
            degree_mutex.ReleaseMutex();
        }

        void sre_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            //
            Console.WriteLine("DET");
            mutex.WaitOne();
            listening_id=next_listening_id;
            mutex.ReleaseMutex();
            //

            degree_mutex.WaitOne();
            if (source != null)
            {
                count = 1;
                degree = source.SoundSourceAngle;
                degree_conf = source.SoundSourceAngleConfidence;
            }

            publisher.publish(new Messages.SpeechDetectedMessage((float)degree));
            degree_mutex.ReleaseMutex();
        }

        void sre_LoadGrammarCompleted(object sender, LoadGrammarCompletedEventArgs e)
        {
            mutex.WaitOne();
            publisher.publish(new Messages.LoadGrammarCompletedMessage(e));
            mutex.ReleaseMutex();
        }
        private static RecognizerInfo GetSpeechRecognizer(bool kinect)
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value = "";
                if (kinect)
                    r.AdditionalInfo.TryGetValue("Kinect", out value);
                return ((kinect) ? ("True".Equals(value, StringComparison.InvariantCultureIgnoreCase)) : true) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            foreach (var v in SpeechRecognitionEngine.InstalledRecognizers())
            {
                Console.WriteLine(v.Description);
            }
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }
        private static RecognizerInfo GetKinectRecognizer(bool a)
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            //foreach (var v in SpeechRecognitionEngine.InstalledRecognizers())
            //{
            //    Console.WriteLine(v.Description);
            //}
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }
        private bool configKinect()
        {
            bool full_intialized = false;
            for (int i = 0; !full_intialized || i<5;i++ )
            {
                try
                {
                    string status;


                    foreach (KinectSensor ks in KinectSensor.KinectSensors)
                        Console.WriteLine(((status = ks.Status.ToString()) != "Connected" ? status : "Kinect Connected"));
                    sensor = (from sensorToCheck in KinectSensor.KinectSensors where sensorToCheck.Status == KinectStatus.Connected select sensorToCheck).FirstOrDefault();
                    sensor.ColorStream.Disable();
                    sensor.SkeletonStream.Disable();

                    sensor.Start();
                    source = sensor.AudioSource;

                    source.EchoCancellationMode = EchoCancellationMode.CancellationAndSuppression; // No AEC for this sample
                    source.BeamAngleMode = BeamAngleMode.Automatic; // 
                   
                    source.AutomaticGainControlEnabled = false; // Important to turn this off for speech recognition                    
                    full_intialized = true;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    for (int j = 5; j > 0; j--)
                    {
                        Console.WriteLine("\t{0}...", j);
                        System.Threading.Thread.Sleep(250);
                    }

                }
            }
            return false;

        }
    }
}

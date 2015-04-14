using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Speech.Recognition;


namespace RobinaSpeechServer.Messages
{
    public class SpeechRecognizedMessage : Message
    {
        public SpeechRecognizedMessage(SpeechRecognizedEventArgs a, float degree,float deg_conf,int _id=0)
            : base("SpeechRecognized")
        {
            result.text = a.Result.Text;
            result.confidence = a.Result.Confidence;
            result.position = degree;
            result.position_confidence = deg_conf;
            result.id = _id;
            try
            {
                result.command = a.Result.Alternates[0].Semantics.Value.ToString();
            }
            catch (Exception)
            {
                result.command = result.text;
            }

        }


        
        private SpeechResult result;

        public SpeechResult Result
        {
            get { return result; }
            set { result = value; }
        }

    }
}

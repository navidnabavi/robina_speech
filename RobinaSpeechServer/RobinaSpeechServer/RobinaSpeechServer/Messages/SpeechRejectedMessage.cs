using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Speech.Recognition;


namespace RobinaSpeechServer.Messages
{
    public class SpeechRejectedMessage:Message
    {
        public SpeechRejectedMessage(SpeechRecognitionRejectedEventArgs a, float degree)
            : base("SpeechRejected")
        {
            result.text = a.Result.Text;
            result.confidence = a.Result.Confidence;
            result.position = degree;
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
            private set { result = value; }
        }
        

    }
}

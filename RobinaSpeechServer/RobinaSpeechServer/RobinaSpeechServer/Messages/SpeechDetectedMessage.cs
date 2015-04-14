using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinaSpeechServer.Messages
{
    public class SpeechDetectedMessage : Message
    {
        public SpeechDetectedMessage(float degree)
            : base("SpeechDetected")
        {
            position = degree;
        }
        private float degree;

        public float position
        {
            get { return degree; }
            private set { degree = value; }
        }
        private float _position_confidence;

        public float position_confidence
        {
            get { return _position_confidence; }
            set { position_confidence = value; }
        }
        


    }
}

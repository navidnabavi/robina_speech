using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinaSpeechServer.Messages
{
    public struct SpeechResult
    {
        public float confidence;
        public string text;
        public string command;
        public float position;
        public float position_confidence;
        public int id;
    }
}

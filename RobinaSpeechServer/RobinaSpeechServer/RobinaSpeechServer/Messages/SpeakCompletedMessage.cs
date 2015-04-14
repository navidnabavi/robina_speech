using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinaSpeechServer.Messages
{
    class SpeakCompletedMessage : Message
    {
        
        public SpeakCompletedMessage(bool s) :base("SpeakCompleted")
        {
            success = s;
        }
        public bool success;
    }
}

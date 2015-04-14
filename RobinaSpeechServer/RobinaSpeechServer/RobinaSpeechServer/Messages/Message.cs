using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinaSpeechServer.Messages
{
    public class Message
    {
        public Message(string source)
        {
            this.source = source;
        }
        private string source;

        public string Source
        {
            get { return source; }
            private set { source = value; }
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Speech.Recognition;

using System.Threading.Tasks;

namespace RobinaSpeechServer.Messages
{
    public class LoadGrammarCompletedMessage : Message
    {
        public LoadGrammarCompletedMessage(LoadGrammarCompletedEventArgs a)
            : base("LoadGrammarCompleted")
        {
            GrammarName = a.Grammar.Name;
         
            Error = (a.Error!=null)?(a.Error.Message):("");
        }
        private string grammarName;

        public string GrammarName
        {
            get { return grammarName; }
            private set { grammarName = value; }
        }
        private string errorMessage;

        public string ErrorMessage
        {
            get { return errorMessage; }
            private set { errorMessage = value; }
        }

        string Error;


    }
}

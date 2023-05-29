using System;
using System.Collections.Generic;
using System.Text;

namespace RemoteAccess
{
    public class RemoteAccessMessage
    {
        public string sender { get; set; }
        public string senderIP { get; set; }

        public string command { get; set; }

        public string commandArg1 { get; set; }
        public string commandArg2 { get; set; }

        public override string ToString()
        {
            return $"Message: Command: {command}  arg1: {commandArg1}  arg2: {commandArg2}";
        }
    }
}

using System;

namespace BackupUtilityCore
{
    public sealed class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string msg)
        {
            Message = msg;
        }

        public string Message
        {
            get;
            private set;
        }
    }
}

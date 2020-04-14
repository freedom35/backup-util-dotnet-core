using System;

namespace BackupUtilityCore
{
    public sealed class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string category, string message)
        {
            Category = category;
            Message = message;
        }

        public string Category
        {
            get;
            private set;

        }

        public string Message
        {
            get;
            private set;
        }
    }
}

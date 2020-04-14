using System;

namespace BackupUtilityCore
{
    /// <summary>
    /// Class definition for message events.
    /// </summary>
    public sealed class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string message, string messageArg)
        {
            Message = message;
            Arg = messageArg;
            
        }

        public string Message
        {
            get;
            private set;
        }

        public string Arg
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return ToString(int.MaxValue);
        }

        public string ToString(int consoleWidth)
        {
            string eventAsString;

            if (!string.IsNullOrEmpty(Arg))
            {
                // Add some padding for consistent output
                string paddedMessage = $"{Message}: ".PadRight(17);
                string arg = Arg;

                eventAsString = $"{paddedMessage}{arg}";

                // Truncate to console buffer width otherwise will overflow onto new line.
                if (eventAsString.Length > consoleWidth)
                {
                    int lengthToRemove = (eventAsString.Length - consoleWidth) + 1;

                    // Check can keep on one line
                    if (Arg.Length > lengthToRemove)
                    {
                        // Truncate arg part
                        arg = Arg.Remove(0, lengthToRemove);

                        // Replace first message char with tilde to indicate truncated
                        eventAsString = $"{paddedMessage}~{arg}";
                    }
                }
            }
            else
            {
                eventAsString = Message;
            }

            return eventAsString;
        }
    }
}

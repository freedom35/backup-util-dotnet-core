using System;

namespace BackupUtilityCore
{
    /// <summary>
    /// Class definition for message event arguments.
    /// </summary>
    public sealed class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string message, string messageArg)
        {
            Message = message;
            Arg = messageArg;
        }

        /// <summary>
        /// Event message.
        /// </summary>
        public string Message
        {
            get;
            private set;
        }

        /// <summary>
        /// Optional arg related to the message.
        /// </summary>
        public string Arg
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns string for class with no limit on length.
        /// </summary>
        public override string ToString()
        {
            return ToString(int.MaxValue);
        }

        /// <summary>
        /// Returns string no longer than the specified max length.
        /// </summary>
        /// <param name="maxLength">Max length of string to return</param>
        /// <returns>Length-limited string</returns>
        public string ToString(int maxLength)
        {
            string eventAsString;

            if (!string.IsNullOrEmpty(Arg))
            {
                const int MinPadding = 8;

                // Add some padding for consistent output
                string paddedMessage = Message.PadRight(MinPadding) + " - ";
                string arg = Arg;

                eventAsString = paddedMessage + arg;

                // Truncate to console buffer width otherwise will overflow onto new line.
                if (eventAsString.Length > maxLength)
                {
                    int lengthToRemove = (eventAsString.Length - maxLength) + 1;

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

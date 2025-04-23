using System;
using System.Runtime.Serialization;

namespace StarmaidIntegrationComputer.Commands
{
    public class CommandFailedException : Exception
    {
        public CommandFailedException(string? message) : base(message)
        {
        }

        public CommandFailedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected CommandFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

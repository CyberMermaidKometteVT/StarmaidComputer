using System;

namespace StarmaidIntegrationComputer.Twitch.Authorization.Exceptions
{
    public class TwitchAuthorizationFailedException : Exception
    {
        public string ErrorCode { get; private set; }

        public TwitchAuthorizationFailedException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}

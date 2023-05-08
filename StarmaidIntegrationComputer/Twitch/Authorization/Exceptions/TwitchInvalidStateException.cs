using System;

namespace StarmaidIntegrationComputer.Twitch.Authorization.Exceptions
{
    public class TwitchInvalidStateException : Exception
    {
        public TwitchInvalidStateException(string expectedState, string observedState) : base(DescribeState(expectedState, observedState))
        {

        }

        private static string DescribeState(string expectedState, string observedState)
        {
            return $"SECURITY ERROR!  CSRF POSSIBLY DETECTED!  The OAuth state observed was not the value we were expecting.\r\nExpected:{expectedState}\r\nObserved: {observedState}";
        }
    }
}

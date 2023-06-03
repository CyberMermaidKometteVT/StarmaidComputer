using System;

namespace StarmaidIntegrationComputer.Commands.Twitch.Enums
{
    [Flags]
    internal enum TwitchStateToValidate
    {
        None = 0,
        Api = 1,
        Chatbot = 2,
        ChatbotAndApi= 3
    }
}

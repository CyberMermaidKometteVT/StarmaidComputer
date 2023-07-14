using System.Collections.Generic;

using StarmaidIntegrationComputer.Common.Settings.Interfaces;

namespace StarmaidIntegrationComputer.StarmaidSettings
{
    //TODO: Consider breaking this out into interfaces to fullfil the Liskov Substitution OOP design principle.
    public class Settings : IOpenAIBearerToken, ISoundPathSettings, IThalassaCoreSettings
    {
        public bool RunOnStartup { get; set; }
        public string TwitchApiUsername { get; set; }
        public string TwitchChatbotUsername { get; set; }
        public string TwitchChatbotChannelName { get; set; }
        public string TwitchClientId { get; set; }
        public string TwitchClientSecret { get; set; }
        public string TwitchApiUrl { get; set; }
        public string DiscordWebhookUrl { get; set; }
        //There is a cleaner way to do the bearer token!  See https://github.com/OkGoDoIt/OpenAI-API-dotnet#authentication
        public string OpenAIBearerToken { get; set; }
        public string JailbreakMessage { get; set; }
        public List<Role> Roles { get; set; }
        public string RedirectUri { get; set; }
        public char ChatCommandIdentifier { get; set; } = '!';
        public char WhisperCommandIdentifier { get; set; } = '!';
        public string StartingListeningSoundPath { get; set; }
        public string StoppingListeningSoundPath { get; set; }
        public float WakeWordConfidenceThreshold { get; set; }
        public bool ForceTwitchLoginPrompt { get; set; } = false;
        public bool LogInWithIncognitoBrowser { get; set; } = true;
    }
}

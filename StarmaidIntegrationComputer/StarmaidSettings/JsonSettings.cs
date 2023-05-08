using System.Collections.Generic;

namespace StarmaidIntegrationComputer.StarmaidSettings
{
    internal class JsonSettings
    {
        public bool RunOnStartup { get; set; }
        public string TwitchApiUsername { get; set; }
        public string TwitchChatbotUsername { get; set; }
        public string TwitchChatbotChannelName { get; set; }
        public string TwitchClientId { get; set; }
        public string TwitchClientSecret { get; set; }
        public string TwitchApiUrl { get; set; }
        public string DiscordWebhookUrl { get; set; }
        public List<Role> Roles { get; set; }
        public string RedirectUri { get; set; }
        public string ChatCommandIdentifier { get; set; }
        public string WhisperCommandIdentifier { get; set; }
    }

}
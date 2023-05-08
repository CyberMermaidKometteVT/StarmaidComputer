using System.Collections.Generic;

namespace StarmaidDiscordLiveNotifier
{
    public class Role
    {
        public string Name { get; set; }
        public ulong Id { get; set; }
    }

    public class Settings
    {
        public string TwitchApiUsername { get; set; }
        public string TwitchChatbotUsername { get; set; }
        public string TwitchChatbotChannelName { get; set; }
        public string TwitchClientId { get; set; }
        public string TwitchClientSecret { get; set; }
        public string TwitchApiUrl { get; set; }
        public string DiscordWebhookUrl { get; set; }
        public List<Role> Roles { get; set; }
    }
}
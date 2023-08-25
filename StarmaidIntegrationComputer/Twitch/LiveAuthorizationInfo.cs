using StarmaidIntegrationComputer.Twitch.Authorization.Models;

namespace StarmaidIntegrationComputer.Twitch
{
    public class LiveAuthorizationInfo
    {
        public string BroadcasterId { get; set; }
        public AccessToken AccessToken { get; set; }
    }
}

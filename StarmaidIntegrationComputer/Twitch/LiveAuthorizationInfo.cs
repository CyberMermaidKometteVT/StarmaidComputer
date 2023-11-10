using StarmaidIntegrationComputer.Twitch.Authorization.Models;

namespace StarmaidIntegrationComputer.Twitch
{
    public class LiveAuthorizationInfo
    {
        //Also a "broadcaster ID"
        public string ThalassaUserId { get; set; }
        
        public string StreamerBroadcasterId { get; set; }
        public AccessToken AccessToken { get; set; }
    }
}

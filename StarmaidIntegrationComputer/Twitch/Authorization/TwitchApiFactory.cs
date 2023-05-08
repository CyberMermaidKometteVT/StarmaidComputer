using System.Collections.Generic;
using System.Linq;

using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;

namespace StarmaidIntegrationComputer.Twitch.Authorization
{
    internal static class TwitchApiFactory
    {
        public static TwitchAPI Build(string clientId, string clientSecret, IEnumerable<AuthScopes> scopes)
        {

            var twitchConnection = new TwitchAPI();
            twitchConnection.Settings.ClientId = clientId;
            twitchConnection.Settings.Secret = clientSecret;
            twitchConnection.Settings.Scopes = scopes.ToList();
            return twitchConnection;
        }
    }
}

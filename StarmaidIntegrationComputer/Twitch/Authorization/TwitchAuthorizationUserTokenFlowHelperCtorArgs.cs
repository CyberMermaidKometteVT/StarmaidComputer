using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;

using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;

namespace StarmaidIntegrationComputer.Twitch.Authorization
{
    public class TwitchAuthorizationUserTokenFlowHelperCtorArgs
    {
        public TwitchSensitiveSettings TwitchSensitiveSettings { get; }
        public TwitchSettings TwitchSettings { get; }
        public ILogger<TwitchAuthorizationUserTokenFlowHelper> Logger { get; }
        public List<AuthScopes> Scopes { get; }
        public TwitchAuthResponseWebserver Webserver { get; }
        public TwitchAPI? TwitchApiConnection { get; }

        public TwitchAuthorizationUserTokenFlowHelperCtorArgs(TwitchSensitiveSettings twitchSensitiveSettings, TwitchSettings twitchSettings, ILogger<TwitchAuthorizationUserTokenFlowHelper> logger, List<AuthScopes> scopes, TwitchAuthResponseWebserver webserver, TwitchAPI? twitchApiConnection)
        {
            TwitchSensitiveSettings = twitchSensitiveSettings;
            TwitchSettings = twitchSettings;
            Logger = logger;
            Scopes = scopes;
            Webserver = webserver;
            TwitchApiConnection = twitchApiConnection;
        }
    }
}

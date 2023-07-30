
using Microsoft.Extensions.Logging;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch.Authorization;
using StarmaidIntegrationComputer.Twitch;
using TwitchLib.Api;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;

namespace StarmaidIntegrationComputer
{
    public class IntegrationComputerCoreCtorArgs
    {
        public ILoggerFactory LoggerFactory { get; }
        public TwitchSensitiveSettings TwitchSensitiveSettings { get; }
        public TwitchSettings TwitchSettings { get; }
        public TwitchAuthorizationUserTokenFlowHelper AuthorizationHelper { get; }
        public TwitchAPI TwitchConnection { get; }
        public SpeechComputer SpeechComputer { get; }
        public StarmaidStateBag StateBag { get; }
        public LiveAuthorizationInfo LiveTwitchAuthorizationInfo { get; }

        public IntegrationComputerCoreCtorArgs(ILoggerFactory loggerFactory, TwitchSensitiveSettings twitchSensitiveSettings, TwitchSettings twitchSettings, TwitchAuthorizationUserTokenFlowHelper authorizationHelper, TwitchAPI twitchConnection, SpeechComputer speechComputer, StarmaidStateBag stateBag, LiveAuthorizationInfo liveTwitchAuthorizationInfo)
        {
            LoggerFactory = loggerFactory;
            TwitchSensitiveSettings = twitchSensitiveSettings;
            TwitchSettings = twitchSettings;
            AuthorizationHelper = authorizationHelper;
            TwitchConnection = twitchConnection;
            SpeechComputer = speechComputer;
            StateBag = stateBag;
            LiveTwitchAuthorizationInfo = liveTwitchAuthorizationInfo;
        }
    }
}

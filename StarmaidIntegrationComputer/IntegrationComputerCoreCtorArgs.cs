
using Microsoft.Extensions.Logging;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch.Authorization;
using StarmaidIntegrationComputer.Twitch;
using TwitchLib.Api;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.UdpThalassaControl;
using StarmaidIntegrationComputer.Common.Settings;

namespace StarmaidIntegrationComputer
{
    public class IntegrationComputerCoreCtorArgs
    {
        public ILoggerFactory LoggerFactory { get; }
        public TwitchSensitiveSettings TwitchSensitiveSettings { get; }
        public TwitchSettings TwitchSettings { get; }
        public ThalassaSettings ThalassaSettings { get; }
        public StreamerProfileSettings StreamerProfileSettings { get; }
        public TwitchAuthorizationUserTokenFlowHelper AuthorizationHelper { get; }
        public TwitchAPI TwitchConnection { get; }
        public SpeechComputer SpeechComputer { get; }
        public StarmaidStateBag StateBag { get; }
        public LiveAuthorizationInfo LiveTwitchAuthorizationInfo { get; }
        public UdpCommandSettings UdpCommandSettings { get; }
        public UdpCommandListener UdpCommandListener { get; }

        public IntegrationComputerCoreCtorArgs(ILoggerFactory loggerFactory, TwitchSensitiveSettings twitchSensitiveSettings, TwitchSettings twitchSettings, ThalassaSettings thalassaSettings, StreamerProfileSettings streamerProfileSettings, TwitchAuthorizationUserTokenFlowHelper authorizationHelper, TwitchAPI twitchConnection, SpeechComputer speechComputer, StarmaidStateBag stateBag, LiveAuthorizationInfo liveTwitchAuthorizationInfo, UdpCommandSettings udpCommandSettings, UdpCommandListener udpCommandListener)
        {
            LoggerFactory = loggerFactory;
            TwitchSensitiveSettings = twitchSensitiveSettings;
            TwitchSettings = twitchSettings;
            ThalassaSettings = thalassaSettings;
            StreamerProfileSettings = streamerProfileSettings;
            AuthorizationHelper = authorizationHelper;
            TwitchConnection = twitchConnection;
            SpeechComputer = speechComputer;
            StateBag = stateBag;
            LiveTwitchAuthorizationInfo = liveTwitchAuthorizationInfo;
            UdpCommandSettings = udpCommandSettings;
            UdpCommandListener = udpCommandListener;
        }
    }
}

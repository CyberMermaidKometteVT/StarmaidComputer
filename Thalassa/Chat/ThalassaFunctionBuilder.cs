using OpenAI.Builders;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;

using StarmaidIntegrationComputer.Commands.Twitch.Enums;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Thalassa.Settings;

namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    public class ThalassaFunctionBuilder
    {
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly ThalassaSettings thalassaSettings;

        public ThalassaFunctionBuilder(StreamerProfileSettings streamerProfileSettings, ThalassaSettings thalassaSettings)
        {
            this.streamerProfileSettings = streamerProfileSettings;
            this.thalassaSettings = thalassaSettings;
        }

        #region Twitch Administrative
        private FunctionDefinition GetShoutoutDefinition() => new FunctionDefinitionBuilder(CommandNames.SHOUTOUT, $"Encourages viewers to follow the target, iff the phrase \"shout out\" is seen. Twitch command.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of target Twitch channel, or the special phrase, \"the last raider\"."))
            .Validate()
            .Build();

        private FunctionDefinition GetTimeoutDefinition() => new FunctionDefinitionBuilder(CommandNames.TIMEOUT, "Blocks the target user from chatting. Only used if \"time out\" is explicitly called for. Twitch command.")
            .AddParameter("target", PropertyDefinition.DefineString("The target Twitch username."))
            .AddParameter("duration", PropertyDefinition.DefineInteger($"Duration of timeout, in seconds. Should default to 300.{thalassaSettings.TimeoutDurationExtraDescription}"))
            .AddParameter("reason", PropertyDefinition.DefineString("Why they're being timed out. This is optional, if no reason has been given, don't make one up."))
            .Validate()
            .Build();

        private FunctionDefinition GetTurnOnShieldModeDefinition() => new FunctionDefinitionBuilder(CommandNames.TURN_ON_SHIELD_MODE, $"Activates Shield Mode, locking down chat in the event that people are misbehaving badly or there is a bad actor present. Twitch command.")
            .Validate()
            .Build();

        private FunctionDefinition GetTurnOffShieldModeDefinition() => new FunctionDefinitionBuilder(CommandNames.TURN_OFF_SHIELD_MODE, $"This will exit Shield Mode, resuming normal chat features. Twitch command.")
            .Validate()
            .Build();
        #endregion Twitch Administrative

        #region Discord
        private FunctionDefinition GetMuteDefinition() => new FunctionDefinitionBuilder(CommandNames.MUTE, $"Mutes streamer's mic. Discord command.")
            .Validate()
            .Build();

        private FunctionDefinition GetUnmuteDefinition() => new FunctionDefinitionBuilder(CommandNames.UNMUTE, $"Unmutes streamer's mic. Discord command.")
            .Validate()
            .Build();

        private FunctionDefinition GetDeafenDefinition() => new FunctionDefinitionBuilder(CommandNames.DEAFEN, $"Deafens streamer, silencing their mic and also their output. Discord command.")
            .Validate()
            .Build();

        #endregion

        #region Informational
        private FunctionDefinition GetIsShieldModeOnDefinition() => new FunctionDefinitionBuilder(CommandNames.IS_SHIELD_MODE_ON, $"Tells whether or not shield mode is on. Twitch command.")
            .Validate()
            .Build();

        private FunctionDefinition GetSayRaiderListDefinition() => new FunctionDefinitionBuilder(CommandNames.SAY_RAIDER_LIST, $"Tells who has raided her Twitch stream since the chatbot was connected to Twitch. Internal chatbot command.")
            .Validate()
            .Build();

        private FunctionDefinition GetSayLastRaiderDefinition() => new FunctionDefinitionBuilder(CommandNames.SAY_LAST_RAIDER, $"Describes the most recent raider since the chatbot was connected to Twitch. Internal chatbot command.")
            .Validate()
            .Build();

        private FunctionDefinition GetSendCannedMessageToChatDefinition() => new FunctionDefinitionBuilder(CommandNames.SEND_CANNED_MESSAGE_TO_CHAT, thalassaSettings.CannedMessageDescription)
            .Validate()
            .Build();

        private FunctionDefinition GetSayLastFollowersDefinition() => new FunctionDefinitionBuilder(CommandNames.SAY_LAST_FOLLWERS, $"Describes the most recent followers, optionally specifying how many.")
            .AddParameter("count", PropertyDefinition.DefineInteger("The number of followers to show, which should default to 5."))
    .Validate()
    .Build();

        #endregion

        public List<FunctionDefinition> BuildStreamerAccessibleFunctions()
        {
            return new List<FunctionDefinition>
            {
                //Twitch
                GetShoutoutDefinition(),
                GetTimeoutDefinition(),
                GetTurnOnShieldModeDefinition(),
                GetTurnOffShieldModeDefinition(),

                //Discord
                GetMuteDefinition(),
                GetUnmuteDefinition(),
                GetDeafenDefinition(),

                //Informational
                GetIsShieldModeOnDefinition(),
                GetSayRaiderListDefinition(),
                GetSayLastRaiderDefinition(),
                GetSendCannedMessageToChatDefinition(),
                GetSayLastFollowersDefinition()
            };
        }
    }
}

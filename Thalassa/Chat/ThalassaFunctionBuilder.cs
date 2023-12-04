using OpenAI.Builders;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;

using StarmaidIntegrationComputer.Common.Settings;

namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    public class ThalassaFunctionBuilder
    {
        private readonly StreamerProfileSettings streamerProfileSettings;

        public ThalassaFunctionBuilder(StreamerProfileSettings streamerProfileSettings)
        {
            this.streamerProfileSettings = streamerProfileSettings;
        }

        private FunctionDefinition GetRaidDefinition() => new FunctionDefinitionBuilder("Raid", $"Raids, sending {streamerProfileSettings.StreamerName}'s community to target channel. Twitch function.")
            .AddParameter("target", PropertyDefinition.DefineString($"The username of target Twitch channel. Very rarely be a viewer or chatter, almost always from favorite streamers list, or an unknown entity. Ask for confirmation before executing if not on favorite streamers list."))
            .Validate()
            .Build();

        private FunctionDefinition GetShoutoutDefinition() => new FunctionDefinitionBuilder("Shoutout", $"Encourages viewers to follow the target, iff the phrase \"shout out\" is seen. Twitch command.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of target Twitch channel, or the special phrase, \"the last raider\"."))
            .Validate()
            .Build();

        private FunctionDefinition GetTimeoutDefinition() => new FunctionDefinitionBuilder("Timeout", "Blocks the target user from chatting. Only used if \"time out\" is explicitly called for, not for teasing or bullying. Twitch command.")
            .AddParameter("target", PropertyDefinition.DefineString("The target Twitch username."))
            .AddParameter("duration", PropertyDefinition.DefineInteger("Duration of timeout, in seconds. Should default to 300, unless target is actuallystan666 being timed out; default for him is 60."))
            .Validate()
            .Build();

        private FunctionDefinition GetMuteDefinition() => new FunctionDefinitionBuilder("Mute", $"Mutes streamer's mic. Discord command.")
            .Validate()
            .Build();

        private FunctionDefinition GetUnmuteDefinition() => new FunctionDefinitionBuilder("Unmute", $"Unmutes streamer's mic. Discord command.")
            .Validate()
            .Build();

        private FunctionDefinition GetDeafenDefinition() => new FunctionDefinitionBuilder("Deafen", $"Deafens streamer, silencing their mic and also their output. Discord command.")
            .Validate()
            .Build();

        private FunctionDefinition GetIsShieldModeOnDefinition() => new FunctionDefinitionBuilder("IsShieldModeOn", $"Tells whether or not shield mode is on. Twitch command.")
            .Validate()
            .Build();

        private FunctionDefinition GetTurnOnShieldModeDefinition() => new FunctionDefinitionBuilder("TurnOnShieldMode", $"Activates Shield Mode, locking down chat in the event that people are misbehaving badly or there is a bad actor present. Twitch command.")
            .Validate()
            .Build();

        private FunctionDefinition GetTurnOffShieldModeDefinition() => new FunctionDefinitionBuilder("TurnOffShieldMode", $"This will exit Shield Mode, resuming normal chat features. Twitch command.")
            .Validate()
            .Build();

        public List<FunctionDefinition> BuildStreamerAccessibleFunctions()
        {
            return new List<FunctionDefinition>
            {
                GetRaidDefinition(),
                GetShoutoutDefinition(),
                GetTimeoutDefinition(),
                GetMuteDefinition(),
                GetUnmuteDefinition(),
                GetDeafenDefinition(),
                GetIsShieldModeOnDefinition(),
                GetTurnOnShieldModeDefinition(),
                GetTurnOffShieldModeDefinition()
            };
        }
    }
}

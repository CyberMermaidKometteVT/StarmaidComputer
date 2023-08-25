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

        private FunctionDefinition GetRaidDefinition() => new FunctionDefinitionBuilder("Raid", $"This will create a raid, from {streamerProfileSettings.StreamerName}'s community to the specified target channel.")
            .AddParameter("target", PropertyDefinition.DefineString($"The username of the Twitch channel that will be the target of the raid. This will very rarely be a viewer or a chatter, almost always someone from {streamerProfileSettings.StreamerName}'s favorite streamers list, or an unknown entity altogether - although if it's not on their favorite streamers list, always prompt before executing!"))
            .Validate()
            .Build();

        private FunctionDefinition GetShoutoutDefinition() => new FunctionDefinitionBuilder("Shoutout", $"This will perform the special Twitch shoutout command, used only if {streamerProfileSettings.StreamerName} has specifically used the phrase \"shoutout\" in their last message, and will not be used in place of a greeting or celebration of a person. This is not the colloquial use of the word \"shoutout\" and being told to send greetings or celebrations to someone should never result in this being called.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of the Twitch channel that will be the target of the shoutout."))
            .Validate()
            .Build();

        private FunctionDefinition GetTimeoutDefinition() => new FunctionDefinitionBuilder("Timeout", "ONLY CALLED IF THE PHRASE 'TIME OUT' IS PROVIDED IN THE PROMPT. Times out the target user, to be called only if the user specifically uses the phrase \"time out\", and will not be used in place of an insult or expression of aggression or any other apparent call to take action. Any other instruction the user provides that does not have the phrase \"time out\" should NOT result in a Timeout() call. It will not be used to impose penalties or discipline unless explicitly called for.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of the Twitch user that will be the target of the timeout, or the phrase, \"the last raider\"."))
            .AddParameter("duration", PropertyDefinition.DefineInteger("The duration of the timeout, in seconds. This should default to 300, unless it's actuallystan666 being timed out, in which case, it should default to 60."))
            .Validate()
            .Build();

        private FunctionDefinition GetMuteDefinition() => new FunctionDefinitionBuilder("Mute", $"This will mute {streamerProfileSettings.StreamerName}'s mic, she will not be audible in their Discord call. This usually means she's about to sneeze or something.")
            .Validate()
            .Build();

        private FunctionDefinition GetUnmuteDefinition() => new FunctionDefinitionBuilder("Unmute", $"This will unmute {streamerProfileSettings.StreamerName}'s mic, she will be audible in their Discord call as normal.")
            .Validate()
            .Build();

        private FunctionDefinition GetDeafenDefinition() => new FunctionDefinitionBuilder("Deafen", $"This will both mute {streamerProfileSettings.StreamerName}'s mic in their Discord call, and also make it so she can't hear anyone else in the call.")
            .Validate()
            .Build();

        private FunctionDefinition GetTurnOnShieldModeDefinition() => new FunctionDefinitionBuilder("TurnOnShieldMode", $"This will lock down {streamerProfileSettings.StreamerName}'s chat in the event that people are misbehaving badly or there is a bad actor present. It's also often used when the streamer is offline.")
            .Validate()
            .Build();

        private FunctionDefinition GetTurnOffShieldModeDefinition() => new FunctionDefinitionBuilder("TurnOffShieldMode", $"This will exit Shield Mode, enabling {streamerProfileSettings.StreamerName}'s community to chat again.")
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
                GetTurnOnShieldModeDefinition(),
                GetTurnOffShieldModeDefinition()
            };
        }
    }
}

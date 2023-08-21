using OpenAI.Builders;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;

namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    internal static class ThalassaFunctionBuilder
    {
        private static FunctionDefinition raidDefinition = new FunctionDefinitionBuilder("Raid", "This will create a raid, from Komette's community to the specified target channel.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of the Twitch channel that will be the target of the raid. This will very rarely be a viewer or a chatter, almost always someone from Komette's favorite streamers list, or an unknown entity altogether - although if it's not on her favorite streamers list, always prompt before executing!"))
            .Validate()
            .Build();

        private static FunctionDefinition shoutoutDefinition = new FunctionDefinitionBuilder("Shoutout", "This will shout out a Twitch user in Komette's chat, or the phrase, \"the last raider\". A shoutout is a positive celebration of that user's stream. It will usually be someone who has raided her, but might be a chatter, or might even be someone who's not currently viewing her stream at all. This is NOT a simple greeting, it is not just a compliment, and it should only be called if she is specifically calling for a shout out, other celebrations of the person such as \"say hi to\" should not cause this to be called.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of the Twitch channel that will be the target of the shoutout."))
            .Validate()
            .Build();

        private static FunctionDefinition timeoutDefinition = new FunctionDefinitionBuilder("Timeout", "Times out the target user, preventing them from chatting for a certain duration.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of the Twitch user that will be the target of the timeout, or the phrase, \"the last raider\"."))
            .AddParameter("duration", PropertyDefinition.DefineInteger("The duration of the timeout, in seconds. This should default to 300, unless it's actuallystan666 being timed out, in which case, it should default to 60."))
            .Validate()
            .Build();

        private static FunctionDefinition muteDefinition = new FunctionDefinitionBuilder("Mute", "This will mute Komette's mic, she will not be audible in her Discord call. This usually means she's about to sneeze or something.")
            .Validate()
            .Build();

        private static FunctionDefinition unmuteDefinition = new FunctionDefinitionBuilder("Unmute", "This will unmute Komette's mic, she will be audible in her Discord call as normal.")
            .Validate()
            .Build();

        private static FunctionDefinition deafenDefinition = new FunctionDefinitionBuilder("Deafen", "This will both mute Komette's mic in her Discord call, and also make it so she can't hear anyone else in the call.")
            .Validate()
            .Build();

        private static FunctionDefinition turnOnShieldModeDefinition = new FunctionDefinitionBuilder("TurnOnShieldMode", "This will lock down Komette's chat in the event that people are misbehaving badly or there is a bad actor present. It's also often used when the streamer is offline.")
            .Validate()
            .Build();

        private static FunctionDefinition turnOffShieldModeDefinition = new FunctionDefinitionBuilder("TurnOffShieldMode", "This will exit Shield Mode, enabling Komette's community to chat again.")
            .Validate()
            .Build();

        public static List<FunctionDefinition> BuildStreamerAccessibleFunctions()
        {
            return new List<FunctionDefinition> { raidDefinition, shoutoutDefinition, timeoutDefinition, muteDefinition, unmuteDefinition, deafenDefinition, turnOnShieldModeDefinition, turnOffShieldModeDefinition };
        }
    }
}

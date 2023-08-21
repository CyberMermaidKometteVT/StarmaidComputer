using OpenAI.Builders;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;

namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    internal static class ThalassaFunctionBuilder
    {
        private static FunctionDefinition raidDefinition = new FunctionDefinitionBuilder("Raid", "This will create a raid, from Komette's community to the specified target channel.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of the Twitch channel that will be the target of the raid."))
            .Validate()
            .Build();

        private static FunctionDefinition shoutoutDefinition = new FunctionDefinitionBuilder("Shoutout", "This will shout out a Twitch user in Komette's chat. It will usually be someone who has raided her, but might be a chatter, or might even be someone who's not currently viewing her stream at all.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of the Twitch channel that will be the target of the shoutout."))
            .Validate()
            .Build();

        private static FunctionDefinition timeoutDefinition = new FunctionDefinitionBuilder("Timeout", "Times out the target user, preventing them from chatting for a certain duration.")
            .AddParameter("target", PropertyDefinition.DefineString("The username of the Twitch user that will be the target of the timeout."))
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

using StarmaidIntegrationComputer.Commands.Twitch.Enums;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Thalassa.Settings;
using OpenAI.Chat;

namespace StarmaidIntegrationComputer.Thalassa.Chat
{
    public class ThalassaToolBuilder
    {
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly ThalassaSettings thalassaSettings;

        public ThalassaToolBuilder(StreamerProfileSettings streamerProfileSettings, ThalassaSettings thalassaSettings)
        {
            this.streamerProfileSettings = streamerProfileSettings;
            this.thalassaSettings = thalassaSettings;
        }

        #region Twitch Administrative
        private ChatTool GetShoutoutDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.SHOUTOUT,
            functionDescription: $"Encourages viewers to follow the target, if the phrase \"shout out\" is seen. Twitch command.",
            functionParameters: StringToArgumentJsonBinaryData(@"
{
    ""type"": ""object"",
    ""properties"": 
    {
        ""target"":
        {
            ""type"": ""string"",
            ""description"": ""The username of target Twitch channel, or the special phrase, \""the last raider\"".""
        }
    }
}
"));

        private ChatTool GetTimeoutDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.TIMEOUT,
            functionDescription: $"Blocks the target user from chatting. Only used if \"time out\" is explicitly called for. Twitch command.",
            functionParameters: StringToArgumentJsonBinaryData(@"
{
    ""type"": ""object"",
    ""properties"": 
    {
        ""target"":
        {
            ""type"": ""string"",
            ""description"": ""The username of target Twitch channel.""
        },
        ""duration"":
        {
            ""type"": ""integer"",
            ""description"": ""Duration of timeout, in seconds. Should default to 300. " + thalassaSettings.TimeoutDurationExtraDescription + @"""
        },
        ""reason"":
        {
            ""type"": ""string"",
            ""description"": ""Why they're being timed out. This is optional, if no reason has been given, don't make one up.""
        }
    }
}
"));

        private ChatTool GetTurnOnShieldModeDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.TURN_ON_SHIELD_MODE,
            functionDescription: $"Activates Shield Mode, locking down chat in the event that people are misbehaving badly or there is a bad actor present. Twitch command.");

        private ChatTool GetTurnOffShieldModeDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.TURN_OFF_SHIELD_MODE,
            functionDescription: $"This will exit Shield Mode, resuming normal chat features. Twitch command.");

        private ChatTool GetResetKruizControlDefinition() => ChatTool.CreateFunctionTool
(functionName: CommandNames.RESET_KRUIZ_CONTROL,
            functionDescription: $"Resets Kruiz Control. This is pronounced 'Cruise Control.' Twitch command.");
        #endregion Twitch Administrative

        #region Twitch
        private ChatTool GetSendChatMessageDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.SEND_CHAT_MESSAGE,
            functionDescription: $"Sends the passed message to chat. Particularly useful during Stream Together sessions. Only use this if explicitly instructed to send a message in chat, not simply to respond to someone who is themselves in chat. Twitch command.",
            functionParameters: StringToArgumentJsonBinaryData(@"
{
    ""type"": ""object"",
    ""properties"": 
    {
        ""message"":
        {
            ""type"": ""string"",
            ""description"": ""The text of the message to send.""
        }
    }
}
"));

        #endregion Twitch

        #region Discord
        private ChatTool GetMuteDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.MUTE,
            functionDescription: $"Mutes streamer's mic. Discord command.");

        private ChatTool GetUnmuteDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.UNMUTE,
            functionDescription: $"Unmutes streamer's mic. Discord command.");

        private ChatTool GetDeafenDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.DEAFEN,
            functionDescription: $"Deafens streamer, silencing their mic and also their output. Discord command.");

        #endregion

        #region Informational
        private ChatTool GetIsShieldModeOnDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.IS_SHIELD_MODE_ON,
            functionDescription: $"Tells whether or not shield mode is on. Twitch command.");

        private ChatTool GetSayRaiderListDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.SAY_RAIDER_LIST,
            functionDescription: $"Tells who has raided her Twitch stream since the chatbot was connected to Twitch. Internal chatbot command.");

        private ChatTool GetSayLastRaiderDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.SAY_LAST_RAIDER,
            functionDescription: $"Describes the most recent raider since the chatbot was connected to Twitch. Internal chatbot command.");

        private ChatTool GetSendCannedMessageToChatDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.SEND_CANNED_MESSAGE_TO_CHAT,
            functionDescription: thalassaSettings.CannedMessageDescription);

        private ChatTool GetSayLastFollowersDefinition() => ChatTool.CreateFunctionTool(functionName: CommandNames.SAY_LAST_FOLLWERS,
            functionDescription: $"Describes the most recent followers, optionally specifying how many.",
            functionParameters: StringToArgumentJsonBinaryData(@"
{
    ""type"": ""object"",
    ""properties"": 
    {
        ""count"":
        {
            ""type"": ""integer"",
            ""description"": ""The number of followers to show, which should default to 5.""
        }
    }
}
"));


        #endregion


        public List<ChatTool> BuildToolsAccessibleByStreamerOrder()
        {
            return new List<ChatTool>
            {
                //Twitch Administrative
                GetShoutoutDefinition(),
                GetTimeoutDefinition(),
                GetTurnOnShieldModeDefinition(),
                GetTurnOffShieldModeDefinition(),
                GetResetKruizControlDefinition(),

                //Twitch
                GetSendChatMessageDefinition(),

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

        internal List<ChatTool> BuildToolsAccessibleDuringStreamerConversation()
        {
            return new List<ChatTool>
            {
                //Twitch
                GetSendChatMessageDefinition()
            };
        }

        private BinaryData StringToArgumentJsonBinaryData(string source)
        {
            string modifiedString = source;
            //string modifiedString = System.Web.HttpUtility.JavaScriptStringEncode(source);
            return BinaryData.FromString(modifiedString);
        }
    }
}

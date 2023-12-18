using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch;
using StarmaidIntegrationComputer.Commands.Twitch.Enums;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands
{
    internal class CommandFactory
    {
        private readonly ILogger<CommandBase> commandLogger;
        private readonly TwitchSensitiveSettings twitchSensitiveSettings;
        private readonly ThalassaSettings thalassaSettings;
        private readonly SpeechComputer speechComputer;
        private readonly TwitchClient chatbot;
        private readonly TwitchAPI twitchApi;
        private readonly StarmaidStateBag stateBag;
        private readonly LiveAuthorizationInfo liveTwitchAuthorizationInfo;

        public const string LAST_RAIDER_VERBIAGE = "the last raider";
        public const int DEFAULT_TIMEOUT_DURATION_IN_SECONDS = 300;

        public CommandFactory(ILogger<CommandBase> logger, TwitchSensitiveSettings twitchSensitiveSettings, ThalassaSettings thalassaSettings, SpeechComputer speechComputer, TwitchClient chatbot, LiveAuthorizationInfo liveTwitchAuthorizationInfo, TwitchAPI twitchApi, StarmaidStateBag stateBag)
        {
            this.commandLogger = logger;
            this.twitchSensitiveSettings = twitchSensitiveSettings;
            this.thalassaSettings = thalassaSettings;
            this.speechComputer = speechComputer;
            this.chatbot = chatbot;
            this.twitchApi = twitchApi;
            this.stateBag = stateBag;
            this.liveTwitchAuthorizationInfo = liveTwitchAuthorizationInfo;
        }

        public CommandBase? Parse(string command, Dictionary<string, object>? arguments)
        {

            command = command.ToLower();
            if (command == CommandNames.SHOUTOUT.ToLower())
            {
                var target = GetTargetFromArguments(arguments);
                target = InterpretShoutoutTarget(target);

                return new ShoutoutCommand(commandLogger, speechComputer, twitchSensitiveSettings, liveTwitchAuthorizationInfo, twitchApi, chatbot, stateBag, target);
            }
            if (command == CommandNames.TIMEOUT.ToLower())
            {
                var target = GetTargetFromArguments(arguments);
                var durationInSeconds = GetDurationFromArguments(arguments) ?? DEFAULT_TIMEOUT_DURATION_IN_SECONDS;
                string? timeoutReason = ParseArgumentAsString(arguments, "reason");

                return new TimeoutCommand(commandLogger, speechComputer, twitchSensitiveSettings, liveTwitchAuthorizationInfo, twitchApi, target, durationInSeconds, timeoutReason);
            }
            if (command == CommandNames.SAY_LAST_RAIDER.ToLower())
            {
                return new SayLastRaiderCommand(commandLogger, speechComputer, stateBag);
            }
            if (command == CommandNames.SAY_RAIDER_LIST.ToLower())
            {
                return new SayRaiderListCommand(commandLogger, speechComputer, stateBag);
            }
            if (command == CommandNames.SEND_CANNED_MESSAGE_TO_CHAT.ToLower())
            {
                return new SendCannedMessageToChatCommand(commandLogger, thalassaSettings, twitchSensitiveSettings, speechComputer, TwitchStateToValidate.Chatbot, liveTwitchAuthorizationInfo, chatbot);
            }
            if (command == CommandNames.SAY_LAST_FOLLWERS.ToLower())
            {
                int count = ParseArgumentAsInt(arguments, "count") ?? 5;
                return new SayLastFollwersCommand(commandLogger, speechComputer, twitchSensitiveSettings, liveTwitchAuthorizationInfo, twitchApi, count);
            }

            return null;
        }

        private int? GetDurationFromArguments(Dictionary<string, object>? arguments, int? defaultValue = null)
        {
            string argumentAsString = ParseArgumentAsString(arguments, "duration");
            if (!string.IsNullOrWhiteSpace(argumentAsString))
            {
                if (int.TryParse(argumentAsString, out var durationInSeconds))
                {
                    return durationInSeconds;
                }
            }
            return null;
        }

        private string GetTargetFromArguments(Dictionary<string, object>? arguments)
        {
            return ParseArgumentAsString(arguments, "target");
        }

        //This is probably more JSON parsing than I really need to do, but at time of development I am tired!
        private string? ParseArgumentAsString(Dictionary<string, object>? arguments, string argumentName)
        {
            object? argumentBoxed = null;
            arguments.TryGetValue(argumentName, out argumentBoxed);
            string? argumentAsString = null;
            if (argumentBoxed != null)
            {
                argumentAsString = ((System.Text.Json.JsonElement)argumentBoxed).ToString();
            }

            return argumentAsString;
        }
        private int? ParseArgumentAsInt(Dictionary<string, object>? arguments, string argumentName)
        {
            object? argumentBoxed = null;
            arguments.TryGetValue(argumentName, out argumentBoxed);
            string? argumentAsString = null;
            if (argumentBoxed != null)
            {
                argumentAsString = ((System.Text.Json.JsonElement)argumentBoxed).ToString();
            }
            int argumentAsInt;
            if (!int.TryParse(argumentAsString, out argumentAsInt))
            {
                return null;
            }

            return argumentAsInt;
        }

        private string InterpretShoutoutTarget(string target)
        {
            if (target == LAST_RAIDER_VERBIAGE)
            {
                if (stateBag.Raiders.Any())
                {
                    target = stateBag.Raiders.Last().RaiderName;
                    commandLogger.LogInformation($"The last raider, by the way, was {target}.");
                }
            }

            return target;
        }
    }
}

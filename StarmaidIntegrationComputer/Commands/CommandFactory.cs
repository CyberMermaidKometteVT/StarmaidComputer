using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch;
using StarmaidIntegrationComputer.Commands.Twitch.Enums;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Common.TasksAndExecution;
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
        private readonly StreamerProfileSettings profileSettings;
        private readonly SpeechComputer speechComputer;
        private readonly TwitchClient chatbot;
        private readonly TwitchAPI twitchApi;
        private readonly AudienceRegistry audienceRegistry;
        private readonly LiveAuthorizationInfo liveTwitchAuthorizationInfo;

        public const string LAST_RAIDER_VERBIAGE = "the last raider";
        public const int DEFAULT_TIMEOUT_DURATION_IN_SECONDS = 300;

        private const string TARGET_ARGUMENT_NAME = "target";

        public CommandFactory(ILogger<CommandBase> logger, TwitchSensitiveSettings twitchSensitiveSettings, ThalassaSettings thalassaSettings, StreamerProfileSettings profileSettings, SpeechComputer speechComputer, TwitchClient chatbot, LiveAuthorizationInfo liveTwitchAuthorizationInfo, TwitchAPI twitchApi, AudienceRegistry audienceRegistry)
        {
            this.commandLogger = logger;
            this.twitchSensitiveSettings = twitchSensitiveSettings;
            this.thalassaSettings = thalassaSettings;
            this.profileSettings = profileSettings;
            this.speechComputer = speechComputer;
            this.chatbot = chatbot;
            this.twitchApi = twitchApi;
            this.audienceRegistry = audienceRegistry;
            this.liveTwitchAuthorizationInfo = liveTwitchAuthorizationInfo;
        }

        public CommandBase? Parse(string command, IList<ThalassaCommandCallArgument>? arguments)
        {

            command = command.ToLower();
            if (command == CommandNames.SHOUTOUT.ToLower())
            {
                string? target = GetTargetFromArguments(arguments);
                target = InterpretShoutoutTarget(target);

                if (string.IsNullOrWhiteSpace(target))
                {
                    return new FailedCommand(commandLogger, speechComputer, $"Failed to parse required argument for command {command}: {TARGET_ARGUMENT_NAME}. See log for additional details.");
                }

                return new ShoutoutCommand(commandLogger, speechComputer, twitchSensitiveSettings, liveTwitchAuthorizationInfo, twitchApi, chatbot, audienceRegistry, target);
            }
            if (command == CommandNames.TIMEOUT.ToLower())
            {
                string? target = GetTargetFromArguments(arguments);
                int durationInSeconds = GetDurationFromArguments(arguments) ?? DEFAULT_TIMEOUT_DURATION_IN_SECONDS;
                string? timeoutReason = ParseArgumentAsString(arguments, "reason");

                if (string.IsNullOrWhiteSpace(target))
                {
                    return new FailedCommand(commandLogger, speechComputer, $"Failed to parse required argument for command {command}: {TARGET_ARGUMENT_NAME}. See log for additional details.");
                }

                return new TimeoutCommand(commandLogger, speechComputer, twitchSensitiveSettings, liveTwitchAuthorizationInfo, twitchApi, target, durationInSeconds, timeoutReason);
            }

            if (command == CommandNames.SEND_CHAT_MESSAGE.ToLower())
            {
                const string MESSAGE_TO_SEND_ARGUMENT_NAME = "message";
                string? messageToSend = ParseArgumentAsString(arguments, MESSAGE_TO_SEND_ARGUMENT_NAME);


                if (string.IsNullOrWhiteSpace(messageToSend))
                {
                    return new FailedCommand(commandLogger, speechComputer, $"Failed to parse required argument for command {command}: {MESSAGE_TO_SEND_ARGUMENT_NAME}. See log for additional details.");
                }

                return new SendChatMessageCommand(commandLogger, speechComputer, twitchSensitiveSettings, liveTwitchAuthorizationInfo, twitchApi, chatbot, profileSettings, messageToSend);
            }

            if (command == CommandNames.RESET_KRUIZ_CONTROL.ToLower())
            {
                return new ResetKruizControlCommand(commandLogger, speechComputer, twitchSensitiveSettings, liveTwitchAuthorizationInfo, twitchApi, chatbot);
            }

            if (command == CommandNames.SAY_LAST_RAIDER.ToLower())
            {
                return new SayLastRaiderCommand(commandLogger, speechComputer, audienceRegistry);
            }
            if (command == CommandNames.SAY_RAIDER_LIST.ToLower())
            {
                return new SayRaiderListCommand(commandLogger, speechComputer, audienceRegistry);
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

        private int? GetDurationFromArguments(IList<ThalassaCommandCallArgument>? arguments, int? defaultValue = null)
        {
            string? argumentAsString = ParseArgumentAsString(arguments, "duration");
            if (!string.IsNullOrWhiteSpace(argumentAsString))
            {
                if (int.TryParse(argumentAsString, out var durationInSeconds))
                {
                    return durationInSeconds;
                }
            }
            return null;
        }

        private string? GetTargetFromArguments(IList<ThalassaCommandCallArgument>? arguments)
        {
            return ParseArgumentAsString(arguments, TARGET_ARGUMENT_NAME);
        }

        //This is probably more JSON parsing than I really need to do, but at time of development I am tired!
        private string? ParseArgumentAsString(IList<ThalassaCommandCallArgument>? arguments, string argumentName)
        {
            string? argumentAsString = null;
            TryGetSerializedValue(arguments, argumentName, out argumentAsString);


            return argumentAsString;
        }
        private int? ParseArgumentAsInt(IList<ThalassaCommandCallArgument>? arguments, string argumentName)
        {
            string? argumentAsString = null;
            TryGetSerializedValue(arguments, argumentName, out argumentAsString);

            int argumentAsInt;
            if (!int.TryParse(argumentAsString, out argumentAsInt))
            {
                return null;
            }

            return argumentAsInt;
        }

        private bool TryGetSerializedValue(IList<ThalassaCommandCallArgument>? arguments, string argumentName, out string? serializedValue)
        {
            var argument = arguments?.Where(argumentCandidate => argumentCandidate.Name == argumentName)?.FirstOrDefault();

            if (argument == null)
            {
                serializedValue = null;
                return false;
            }

            serializedValue = argument.SerializedValue;
            return true;
        }

        private string? InterpretShoutoutTarget(string? target)
        {

            if (target == LAST_RAIDER_VERBIAGE)
            {
                if (audienceRegistry.Raiders.Any())
                {
                    target = audienceRegistry.Raiders.Last().RaiderName;
                    commandLogger.LogInformation($"The last raider, by the way, was {target}.");
                }
            }

            return target;
        }
    }
}

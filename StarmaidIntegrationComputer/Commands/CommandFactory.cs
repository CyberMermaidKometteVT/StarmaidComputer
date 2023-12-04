using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.StarmaidSettings;
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
        private readonly SpeechComputer speechComputer;
        private readonly TwitchClient chatbot;
        private readonly TwitchAPI twitchApi;
        private readonly StarmaidStateBag stateBag;
        private readonly LiveAuthorizationInfo liveTwitchAuthorizationInfo;

        public const string LAST_RAIDER_VERBIAGE = "the last raider";
        public const int DEFAULT_TIMEOUT_DURATION_IN_SECONDS = 300;

        public CommandFactory(ILogger<CommandBase> logger, TwitchSensitiveSettings twitchSensitiveSettings, SpeechComputer speechComputer, TwitchClient chatbot, LiveAuthorizationInfo liveTwitchAuthorizationInfo, TwitchAPI twitchApi, StarmaidStateBag stateBag)
        {
            this.commandLogger = logger;
            this.twitchSensitiveSettings = twitchSensitiveSettings;
            this.speechComputer = speechComputer;
            this.chatbot = chatbot;
            this.twitchApi = twitchApi;
            this.stateBag = stateBag;
            this.liveTwitchAuthorizationInfo = liveTwitchAuthorizationInfo;
        }

        public CommandBase? Parse(string command, Dictionary<string, object>? arguments)
        {

            command = command.ToLower();
            if (command == "shoutout")
            {
                var target = GetTargetFromArguments(arguments);
                target = InterpretShoutoutTarget(target);

                return new ShoutoutCommand(commandLogger, speechComputer, twitchSensitiveSettings, chatbot, liveTwitchAuthorizationInfo, twitchApi, stateBag, target);
            }
            if (command == "timeout")
            {
                var target = GetTargetFromArguments(arguments);
                var durationInSeconds = GetDurationFromArguments(arguments) ?? DEFAULT_TIMEOUT_DURATION_IN_SECONDS;

                return new TimeoutCommand(commandLogger, speechComputer, twitchSensitiveSettings, chatbot, liveTwitchAuthorizationInfo, twitchApi, target, durationInSeconds);
            }
            if (command == "saylastraider")
            {
                return new SayLastRaiderCommand(commandLogger, speechComputer, stateBag);
            }
            if (command == "sayraiderlist")
            {
                return new SayRaiderListCommand(commandLogger, speechComputer, stateBag);
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

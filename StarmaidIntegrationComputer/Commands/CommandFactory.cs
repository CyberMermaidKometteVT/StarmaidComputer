using System.Linq;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch;
using StarmaidIntegrationComputer.Common.DataStructures;
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
        private readonly Settings settings;
        private readonly SpeechComputer speechComputer;
        private readonly TwitchClient chatbot;
        private readonly TwitchAPI twitchApi;
        private readonly StarmaidStateBag stateBag;
        private readonly LiveAuthorizationInfo liveTwitchAuthorizationInfo;

        public const string LAST_RAIDER_VERBIAGE = "the last raider";

        public CommandFactory(ILogger<CommandBase> logger, Settings settings, SpeechComputer speechComputer, TwitchClient chatbot, LiveAuthorizationInfo liveTwitchAuthorizationInfo, TwitchAPI twitchApi, StarmaidStateBag stateBag)
        {
            this.commandLogger = logger;
            this.settings = settings;
            this.speechComputer = speechComputer;
            this.chatbot = chatbot;
            this.twitchApi = twitchApi;
            this.stateBag = stateBag;
            this.liveTwitchAuthorizationInfo = liveTwitchAuthorizationInfo;
        }

        public CommandBase? Parse(string command, string target)
        {
            command = command.ToLower();
            if (command == "shoutout")
            {
                InterpretShoutoutTarget(target);

                return new ShoutoutCommand(commandLogger, speechComputer, settings, chatbot, liveTwitchAuthorizationInfo, twitchApi, target);
            }

            return null;
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

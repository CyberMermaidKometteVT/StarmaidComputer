using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Commands.Twitch;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

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

        public CommandFactory(ILogger<CommandBase> logger, Settings settings, SpeechComputer speechComputer, TwitchClient chatbot, TwitchAPI twitchApi)
        {
            this.commandLogger = logger;
            this.settings = settings;
            this.speechComputer = speechComputer;
            this.chatbot = chatbot;
            this.twitchApi = twitchApi;
        }

        public CommandBase Parse(string command, string target)
        {
            if (command == "shoutout")
            {
                return new ShoutoutCommand(commandLogger, speechComputer, settings, chatbot, twitchApi, target);
            }



            return null;
        }
    }
}

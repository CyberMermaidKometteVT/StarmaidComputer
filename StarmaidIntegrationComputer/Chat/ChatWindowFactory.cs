using Microsoft.Extensions.Logging;

using OpenAI.Managers;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Thalassa;
using StarmaidIntegrationComputer.Thalassa.Chat;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;

namespace StarmaidIntegrationComputer.Chat
{
    public class ChatWindowFactory
    {
        private readonly StarmaidStateBag stateBag;
        private readonly OpenAISettings openAISettings;
        private readonly ILogger<ChatComputer> logger;
        private readonly SoundEffectPlayer soundEffectPlayer;
        private readonly ThalassaCore thalassaCore;
        private readonly SpeechComputer speechComputer;
        private readonly VoiceListener voiceListener;
        private readonly OpenAIService openAIService;
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly ThalassaFunctionBuilder thalassaFunctionBuilder;

        public ChatWindowFactory(StarmaidStateBag stateBag, ILogger<ChatComputer> logger, OpenAISettings openAISettings, SoundEffectPlayer soundEffectPlayer, ThalassaCore thalassaCore, SpeechComputer speechComputer, VoiceListener voiceListener, OpenAIService openAIService, StreamerProfileSettings streamerProfileSettings, ThalassaFunctionBuilder thalassaFunctionBuilder)
        {
            this.stateBag = stateBag;
            this.logger = logger;
            this.soundEffectPlayer = soundEffectPlayer;
            this.thalassaCore = thalassaCore;
            this.speechComputer = speechComputer;
            this.voiceListener = voiceListener;
            this.openAIService = openAIService;
            this.streamerProfileSettings = streamerProfileSettings;
            this.thalassaFunctionBuilder = thalassaFunctionBuilder;
            this.openAISettings = openAISettings;
        }

        public ChatWindow CreateNew()
        {
            var args = new ChatWindowCtorArgs
            {
                StateBag = stateBag,
                Logger = logger,
                OpenAISettings = openAISettings,
                SoundEffectPlayer = soundEffectPlayer,
                ThalassaCore = thalassaCore,
                SpeechComputer = speechComputer,
                VoiceListener = voiceListener,
                OpenAIService = openAIService,
                StreamerProfileSettings = streamerProfileSettings,
                ThalassaFunctionBuilder = thalassaFunctionBuilder
            };

            return new ChatWindow(args);
        }
    }
}

using Microsoft.Extensions.Logging;

using OpenAI_API;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Thalassa;
using StarmaidIntegrationComputer.Thalassa.Chat;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;

namespace StarmaidIntegrationComputer.Chat
{
    public class ChatWindowFactory
    {
        private readonly OpenAIAPI api;
        private readonly StarmaidStateBag stateBag;
        private readonly OpenAISettings openAISettings;
        private readonly ILogger<ChatComputer> logger;
        private readonly SoundEffectPlayer soundEffectPlayer;
        private readonly ThalassaCore thalassaCore;
        private readonly SpeechComputer speechComputer;
        private readonly VoiceListener voiceListener;

        public ChatWindowFactory(OpenAIAPI api, StarmaidStateBag stateBag, ILogger<ChatComputer> logger, OpenAISettings openAISettings, SoundEffectPlayer soundEffectPlayer, ThalassaCore thalassaCore, SpeechComputer speechComputer, VoiceListener voiceListener)
        {
            this.api = api;
            this.stateBag = stateBag;
            this.logger = logger;
            this.soundEffectPlayer = soundEffectPlayer;
            this.thalassaCore = thalassaCore;
            this.speechComputer = speechComputer;
            this.voiceListener = voiceListener;
            this.openAISettings = openAISettings;
        }

        public ChatWindow CreateNew()
        {
            var args = new ChatWindowCtorArgs
            {
                Api = api,
                StateBag = stateBag,
                Logger = logger,
                OpenAISettings = openAISettings,
                SoundEffectPlayer = soundEffectPlayer,
                ThalassaCore = thalassaCore,
                SpeechComputer = speechComputer,
                VoiceListener = voiceListener
            };

            return new ChatWindow(args);
        }
    }
}

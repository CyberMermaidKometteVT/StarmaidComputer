using Microsoft.Extensions.Logging;

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
        private readonly AudienceRegistry audienceRegistry;
        private readonly OpenAISettings openAISettings;
        private readonly ILogger<ChatComputer> logger;
        private readonly SoundEffectPlayer soundEffectPlayer;
        private readonly ThalassaCore thalassaCore;
        private readonly SpeechComputer speechComputer;
        private readonly VoiceListener voiceListener;
        private readonly OpenAISensitiveSettings openAISensitiveSettings;
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly ThalassaToolBuilder thalassaFunctionBuilder;

        public ChatWindowFactory(AudienceRegistry audienceRegistry, ILogger<ChatComputer> logger, OpenAISettings openAISettings, SoundEffectPlayer soundEffectPlayer, ThalassaCore thalassaCore, SpeechComputer speechComputer, VoiceListener voiceListener, OpenAISensitiveSettings openAISensitiveSettings, StreamerProfileSettings streamerProfileSettings, ThalassaToolBuilder thalassaFunctionBuilder)
        {
            this.audienceRegistry = audienceRegistry;
            this.logger = logger;
            this.soundEffectPlayer = soundEffectPlayer;
            this.thalassaCore = thalassaCore;
            this.speechComputer = speechComputer;
            this.voiceListener = voiceListener;
            this.openAISensitiveSettings = openAISensitiveSettings;
            this.streamerProfileSettings = streamerProfileSettings;
            this.thalassaFunctionBuilder = thalassaFunctionBuilder;
            this.openAISettings = openAISettings;
        }

        public ChatWindow CreateNew()
        {
            var args = new ChatWindowCtorArgs
            {
                AudienceRegistry = audienceRegistry,
                Logger = logger,
                OpenAISettings = openAISettings,
                SoundEffectPlayer = soundEffectPlayer,
                ThalassaCore = thalassaCore,
                SpeechComputer = speechComputer,
                VoiceListener = voiceListener,
                OpenAISensitiveSettings = openAISensitiveSettings,
                StreamerProfileSettings = streamerProfileSettings,
                ThalassaFunctionBuilder = thalassaFunctionBuilder
            };

            return new ChatWindow(args);
        }
    }
}

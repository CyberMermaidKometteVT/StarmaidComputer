
using Microsoft.Extensions.Logging;
using StarmaidIntegrationComputer.Thalassa.Chat;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;
using StarmaidIntegrationComputer.Thalassa;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Common.Settings;

namespace StarmaidIntegrationComputer.Chat
{
    public class ChatWindowCtorArgs
    {
        public AudienceRegistry AudienceRegistry { get; set; }
        public ILogger<ChatComputer> Logger { get; set; }
        public OpenAISettings OpenAISettings { get; set; }
        public SoundEffectPlayer SoundEffectPlayer { get; set; }
        public ThalassaCore ThalassaCore { get; set; }
        public SpeechComputer SpeechComputer { get; set; }
        public VoiceListener VoiceListener { get; set; }
        public OpenAISensitiveSettings OpenAISensitiveSettings { get; set; }
        public StreamerProfileSettings StreamerProfileSettings { get; set; }
        public ThalassaToolBuilder ThalassaFunctionBuilder { get; set; }
    }
}

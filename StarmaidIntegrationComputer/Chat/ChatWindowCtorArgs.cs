
using Microsoft.Extensions.Logging;
using OpenAI_API;
using StarmaidIntegrationComputer.Thalassa.Chat;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;
using StarmaidIntegrationComputer.Thalassa;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Thalassa.Settings;

namespace StarmaidIntegrationComputer.Chat
{
    public class ChatWindowCtorArgs
    {
        public OpenAIAPI Api { get; set; }
        public StarmaidStateBag StateBag { get; set; }
        public ILogger<ChatComputer> Logger { get; set; }
        public OpenAISettings OpenAISettings { get; set; }
        public SoundEffectPlayer SoundEffectPlayer { get; set; }
        public ThalassaCore ThalassaCore { get; set; }
        public SpeechComputer SpeechComputer { get; set; }
        public VoiceListener VoiceListener { get; set; }
    }
}

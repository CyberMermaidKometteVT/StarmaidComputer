namespace StarmaidIntegrationComputer.Thalassa.Settings
{
    /// <summary>
    /// Configuration for the ViolaWake-trained ONNX wake word pipeline (see
    /// <see cref="WakeWordProcessor.ViolaWakeWordProcessor"/>). Only used when
    /// ThalassaSettings.WakeWordSelectedInterpreter is "ViolaWake".
    /// </summary>
    public class ViolaWakeSettings
    {
        public string? WakeWordModelPath { get; set; }
        public float WakeWordConfidenceThreshold { get; set; }

        public string? CancelListeningModelPath { get; set; }
        public float CancelListeningConfidenceThreshold { get; set; }

        public string? AbortCommandModelPath { get; set; }
        public float AbortCommandConfidenceThreshold { get; set; }
    }
}

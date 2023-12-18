﻿namespace StarmaidIntegrationComputer.Thalassa.Settings
{
    public class ThalassaSettings
    {
        public float WakeWordConfidenceThreshold { get; set; }
        public float CancelListeningConfidenceThreshold { get; set; }
        public float AbortCommandConfidenceThreshold { get; set; }

        public string CannedMessageText { get; set; }
        public string CannedMessageDescription { get; set; }
    }
}

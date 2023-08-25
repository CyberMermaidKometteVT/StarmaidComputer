namespace StarmaidIntegrationComputer.Common.Settings
{
    public class StreamerProfileSettings
    {
        public string StreamerName { get; set; }
        public string StreamerDescription { get; set; }
        public string StreamerMetaDescription { get; set; }
        public string AiName { get; set; }
        public string AiDescription { get; set; }
        public List<string> WakeWords { get; set; }
        public List<string> AbortPhrases { get; set; }
        public List<string> WakeWordSoundalikes { get; set; }
    }
}

namespace StarmaidIntegrationComputer.Common.DataStructures.Audience
{
    public class ChatterMessageInfo
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString() =>
            $"Timestamp: {Timestamp}, Message: {Message}";
    }
}

namespace StarmaidIntegrationComputer.Common.DataStructures.Audience
{
    public class RaiderInfo : IComparable<RaiderInfo>
    {
        public string RaiderName { get; set; }
        public DateTime? LastShoutedOut { get; set; }
        public DateTime RaidTime { get; set; }

        public override string ToString() =>
            $"RaiderName: {RaiderName}, RaidTime: {RaidTime}, LastShoutedOut: {LastShoutedOut?.ToString() ?? "never"}";

        public int CompareTo(RaiderInfo? other)
        {
            if (RaidTime < other.RaidTime)
            {
                return -1;
            }

            if (RaidTime > other.RaidTime)
            {
                return 1;
            }

            return RaiderName.CompareTo(other.RaiderName);
        }
    }
}

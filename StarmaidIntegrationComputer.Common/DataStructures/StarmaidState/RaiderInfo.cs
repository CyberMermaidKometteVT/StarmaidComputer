namespace StarmaidIntegrationComputer.Common.DataStructures.StarmaidState
{
    public class RaiderInfo : IComparable<RaiderInfo>
    {
        public string RaiderName { get; set; }
        public DateTime? LastShoutedOut { get; set; }
        public DateTime RaidTime { get; set; }

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

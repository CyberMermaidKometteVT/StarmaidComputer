namespace StarmaidIntegrationComputer.Common.DataStructures
{
    public class StarmaidStateBag
    {
        //public RaiderList Raiders { get; } = new RaiderList();
        public SortedSet<RaiderInfo> Raiders { get; } = new SortedSet<RaiderInfo>();
        public List<string> Viewers { get; } = new List<string>();
        public List<Chatter> Chatters { get; } = new List<Chatter>();
    }
}

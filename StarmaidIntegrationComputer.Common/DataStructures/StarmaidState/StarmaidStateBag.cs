namespace StarmaidIntegrationComputer.Common.DataStructures.StarmaidState
{
    public class StarmaidStateBag
    {
        public SortedSet<RaiderInfo> Raiders { get; } = new SortedSet<RaiderInfo>();
        public List<string> Viewers { get; } = new List<string>();
        public List<Chatter> Chatters { get; } = new List<Chatter>();
    }
}

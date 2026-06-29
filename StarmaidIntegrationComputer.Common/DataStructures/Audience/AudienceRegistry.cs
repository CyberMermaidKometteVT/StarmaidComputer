using StarmaidIntegrationComputer.Common.DataStructures.Pronouns;

namespace StarmaidIntegrationComputer.Common.DataStructures.Audience
{
    public class AudienceRegistry
    {
        public SortedSet<RaiderInfo> Raiders { get; } = new SortedSet<RaiderInfo>();
        public List<string> Viewers { get; } = new List<string>();
        public List<Chatter> Chatters { get; } = new List<Chatter>();
        public Dictionary<string, PronounInformation?> PronounsByUsername = new Dictionary<string, PronounInformation?>();
        public List<string> UsersToRecheckPronounsFor = new List<string>();
    }
}

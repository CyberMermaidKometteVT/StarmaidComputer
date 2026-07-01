using StarmaidIntegrationComputer.Common.DataStructures.Pronouns;

namespace StarmaidIntegrationComputer.Common.DataStructures.Audience
{
    public class AudienceRegistry
    {
        public SortedSet<RaiderInfo> Raiders { get; } = new SortedSet<RaiderInfo>();
        public List<string> Viewers { get; } = new List<string>();
        public List<Chatter> Chatters { get; } = new List<Chatter>();
        public Dictionary<string, UserAndPronouns?> PronounsByUsername = new Dictionary<string, UserAndPronouns?>();
        public List<string> UsersToRecheckPronounsFor = new List<string>();

        public override string ToString() =>
            $"Raiders: {Raiders.Count}, Chatters: {Chatters.Count}, Viewers: {Viewers.Count}, PronounsCached: {PronounsByUsername.Count}";
    }
}

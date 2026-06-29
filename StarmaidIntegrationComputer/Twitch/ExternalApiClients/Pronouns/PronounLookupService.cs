using System.Collections.Generic;
using System.Threading.Tasks;

using StarmaidIntegrationComputer.Common.DataStructures.Audience;
using StarmaidIntegrationComputer.Common.DataStructures.Pronouns;

namespace StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns
{
    public class PronounLookupService
    {
        internal const string TheyThemKey = "theythem";

        private readonly AudienceRegistry audienceRegistry;
        private readonly PronounsClient pronounsClient;

        public PronounLookupService(AudienceRegistry audienceRegistry, PronounsClient pronounsClient)
        {
            this.audienceRegistry = audienceRegistry;
            this.pronounsClient = pronounsClient;
        }

        public async Task EnsurePronounsAreCachedAsync(IEnumerable<string> usernames)
        {
            foreach (string username in usernames)
            {
                if (!audienceRegistry.PronounsByUsername.ContainsKey(username))
                {
                    var userAndPronouns = await pronounsClient.GetUserAndPronouns(username);
                    if (userAndPronouns != null)
                        audienceRegistry.PronounsByUsername[username] = userAndPronouns.FirstPronoun;
                }
            }
        }

        public async Task FetchAndCacheIfNeededAsync(string username)
        {
            if (audienceRegistry.PronounsByUsername.ContainsKey(username) && !audienceRegistry.UsersToRecheckPronounsFor.Contains(username))
                return;

            var userAndPronouns = await pronounsClient.GetUserAndPronouns(username);
            if (userAndPronouns == null)
            {
                if (!audienceRegistry.UsersToRecheckPronounsFor.Contains(username))
                    audienceRegistry.UsersToRecheckPronounsFor.Add(username);
            }
            else
            {
                audienceRegistry.UsersToRecheckPronounsFor.Remove(username);
                audienceRegistry.PronounsByUsername[username] = userAndPronouns.FirstPronoun;
            }
        }

        public async Task<string> GetPronounLabelOrEmptyString(string username)
        {
            await FetchAndCacheIfNeededAsync(username);
            if (audienceRegistry.PronounsByUsername.TryGetValue(username, out PronounInformation? pronounInfo) && pronounInfo != null)
                return $" ({pronounInfo.Shorthand})";
            return "";
        }

        public async Task<PronounInformation?> GetPronounInformation(string username, bool fallbackToTheyThem)
        {
            await FetchAndCacheIfNeededAsync(username);
            if (audienceRegistry.PronounsByUsername.TryGetValue(username, out PronounInformation? pronounInfo) && pronounInfo != null)
                return pronounInfo;
            if (fallbackToTheyThem)
            {
                if (pronounsClient.PronounTable != null && !pronounsClient.PronounTable.ContainsKey(TheyThemKey))
                {
                    return pronounsClient.PronounTable[TheyThemKey];
                }
            }
            return new PronounInformation(name: "theythem", subject: "They", @object: "Them", singular: false);
        }

        public async Task<(string subjectPronoun, string wasWere)> GetSubjectPronounWithPastTenseVerbFallbackToThey(string username)
        {
            await FetchAndCacheIfNeededAsync(username);
            if (audienceRegistry.PronounsByUsername.TryGetValue(username, out PronounInformation? pronounInfo) && pronounInfo != null)
            {
                string wasWere = pronounInfo.Name == TheyThemKey ? "were" : "was";
                return (pronounInfo.Subject, wasWere);
            }
            return ("they", "were");
        }
    }
}

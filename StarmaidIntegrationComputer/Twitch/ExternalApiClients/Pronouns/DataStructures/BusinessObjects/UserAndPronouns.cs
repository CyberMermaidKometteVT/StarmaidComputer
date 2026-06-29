using StarmaidIntegrationComputer.Common.DataStructures.Pronouns;

namespace StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns.DataStructures.BusinessObjects
{
    public class UserAndPronouns
    {
        public string User { get; }
        public PronounInformation? FirstPronoun { get; }
        public PronounInformation? SecondPronoun { get; }

        public UserAndPronouns(string user, PronounInformation? firstPronoun, PronounInformation? secondPronoun)
        {
            User = user;
            FirstPronoun = firstPronoun;
            SecondPronoun = secondPronoun;
        }

        public string DisplayString
        {
            get
            {
                if(FirstPronoun?.Subject == null)
                {
                    return $"{User}";
                }

                string secondHalfOfPronouns = $"{SecondPronoun?.Subject ?? FirstPronoun.Object}";
                return $"{User} ({FirstPronoun.Subject}/{secondHalfOfPronouns})";
            }
        }

    }
}

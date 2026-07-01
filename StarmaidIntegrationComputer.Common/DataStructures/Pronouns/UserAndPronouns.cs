namespace StarmaidIntegrationComputer.Common.DataStructures.Pronouns
{
    public class UserAndPronouns
    {
        public string Username { get; }
        public PronounInformation? FirstPronoun { get; }
        public PronounInformation? SecondPronoun { get; }

        public UserAndPronouns(string username, PronounInformation? firstPronoun, PronounInformation? secondPronoun)
        {
            Username = username;
            FirstPronoun = firstPronoun;
            SecondPronoun = secondPronoun;
        }

        public string DisplayString
        {
            get
            {
                if (FirstPronoun?.Subject == null)
                    return Username;

                string secondHalf = SecondPronoun?.Subject ?? FirstPronoun.Object;
                return $"{Username} ({FirstPronoun.Subject}/{secondHalf})";
            }
        }

        public override string ToString() =>
            $"Username: {Username}, FirstPronoun: {FirstPronoun?.Shorthand ?? "none"}, SecondPronoun: {SecondPronoun?.Shorthand ?? "none"}";
    }
}

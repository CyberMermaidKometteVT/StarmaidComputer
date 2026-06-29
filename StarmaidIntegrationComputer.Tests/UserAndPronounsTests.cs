using Microsoft.VisualStudio.TestTools.UnitTesting;

using StarmaidIntegrationComputer.Common.DataStructures.Pronouns;
using StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns.DataStructures.BusinessObjects;

namespace StarmaidIntegrationComputer.Tests
{
    [TestClass]
    public class UserAndPronounsTests
    {
        [TestMethod]
        public void DisplayString_NoPronouns_ReturnsJustUserName()
        {
            var userAndPronouns = new UserAndPronouns(
                "TestUser",
                null,
                null
            );
            var displayString = userAndPronouns.DisplayString;
            Assert.AreEqual("TestUser", displayString);
        }

        [TestMethod]
        public void DisplayString_OnePronoun_ReturnsUserNameAndPronoun()
        {
            var userAndPronouns = new UserAndPronouns(
                "TestUser",
                new PronounInformation("theythem", "they", "them", false),
                null
            );
            var displayString = userAndPronouns.DisplayString;
            Assert.AreEqual("TestUser (they/them)", displayString);
        }

        [TestMethod]
        public void DisplayString_TwoPronouns_ReturnsUserNameAndBothPronouns()
        {
            var userAndPronouns = new UserAndPronouns(
                "TestUser",
                new PronounInformation("theythem", "they", "them", false),
                new PronounInformation("sheher", "she", "her", false)
            );
            var displayString = userAndPronouns.DisplayString;
            Assert.AreEqual("TestUser (they/she)", displayString);
        }
    }
}

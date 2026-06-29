using Newtonsoft.Json;

namespace StarmaidIntegrationComputer.Common.DataStructures.Pronouns
{
    public class PronounInformation
    {
        /// <summary>
        /// Returns the key of the pronoun in the pronoun information table.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the subject pronoun in lowercase ("she", "he", "they").
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Returns the subject pronoun with a capitalized first letter ("She", "He", "They").
        /// </summary>
        public string SubjectCapitalized => char.ToUpper(Subject[0]) + Subject[1..];

        /// <summary>
        /// Returns the object pronoun in lowercase ("her", "him", "them").
        /// </summary>
        public string Object { get; }

        /// <summary>
        /// Returns the object pronoun with a capitalized first letter ("Her", "Him", "Them").
        /// </summary>
        public string ObjectCapitalized => char.ToUpper(Object[0]) + Object[1..];

        /// <summary>
        /// TRUE for meta-entries like "any" and "other" that are displayed as a single word rather than a subject/object pair.
        /// Does NOT indicate grammatical number — she/her and he/him are both false.
        /// </summary>
        public bool Singular { get; }

        [JsonConstructor]
        public PronounInformation(string name, string subject, string @object, bool singular)
        {
            Name = name;
            Subject = subject.ToLowerInvariant();
            Object = @object.ToLowerInvariant();
            Singular = singular;
        }

        public string Shorthand => Singular ? Subject : $"{Subject}/{Object}";
    }
}

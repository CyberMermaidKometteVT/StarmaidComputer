﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarmaidIntegrationComputer.Commands.Twitch.CommandHelpers
{
    internal static  class TwitchTagDescriber
    {

        private static IReadOnlyList<string> exactQueerLabels = new List<string> { "LGBTQUIAPLUS", "QUEER", "LESBIAN", "GAY", "GAYMER", "BISEXUAL", "BI", "PANSEXUAL", "INTERSEX", "ACE", "ASEXUAL", "AROMANTIC", "GRAYACE", "GREYACE", "GENDERFLUID", "TRANSBIAN", "NONBINARY", "ENBY", "NB", "GENDERSPICY", "FRIENDOFDOROTHY", "FRIENDOFMARA" }.AsReadOnly();
        private static IReadOnlyList<string> transLabels = new List<string> { "TRANSGENDER", "TRANSBIAN", "TRANSMAN", "TRANSWOMAN" }.AsReadOnly();
        private static IReadOnlyList<string> vtuberLabels = new List<string> { "VTUBER", "PNGTUBER", "LEWDTUBER", "GIFTUBER" }.AsReadOnly();
        private static IReadOnlyList<string> exactArtistLabels = new List<string> { "ART", "ARTIST", "PAINTER", "PAINTING", "MINIATUREPAINTING", "DIGITALARTIST" }.AsReadOnly();
        private static IReadOnlyList<string> musicianLabels = new List<string> { "SONGWRIT", "MUSICIAN", "UKELELE", "GUITAR", "PIANO", "VIOLIN", "FLUTE", "FLOUTIST",  }.AsReadOnly();
        private static IReadOnlyList<string> eighteenPlusLabels = new List<string> { "18PLUS", "NOMINORS", "ADULT", "LEWDTUBER" }.AsReadOnly();
        private static IReadOnlyList<string> neurodivergentLabels = new List<string> { "ADHD", "ASPERGER", "ASPERGERS", "ASPERGER'S", "AUTISTIC", "AUTISM", "AUDHD", "ANXIETY", "ANXIOUS", "BIPOLAR", "BPD", "BORDERLINE", "DEPRESSED", "DEPRESSIVE", "DEPRESSION", "DID", "DISASSOCIATIVE", "DISASSOCIATIVEIDENTITY", "DISASSOCIATIVEIDENTITYDISORDER", "DOWNSYNDROME", "DYSCALCULIA", "DYSLEXIA", "EPILEPSY", "EPILEPTIC", "MENTALHEALTH", "MISOPHONIA", "OBSESSIVECOMPULSIVEDISORDER", "OCD", "PTSD", "CPTSD", "C-PTSD", "PARKINSON", "PARKINSONS", "PARKINSON'S", "SCHIZOPHRENIA", "SYNESTHESIA", "TOURETTE", "TOURETTES", "TOURETTE'S" }.AsReadOnly();

        private static IReadOnlyList<char> vowels = new List<char> ("aeiou").AsReadOnly();


        public static string GetInterestingTagCommentary(IEnumerable<string> tags)
        {
            tags = tags.Select(tag => tag.ToUpper());

            StringBuilder interestingTagCommentary = new StringBuilder();
            bool isCozy = tags.Any(tag => tag.ToUpper().StartsWith("COZY") || tag.ToUpper().StartsWith("COMFY"));
            bool isLGBTQ = tags.Any(DoesTagDescribeQueerStreamer);
            bool isTrans = tags.Any(DoesTagListContainAnyLabelsAsSubstrings(transLabels));
            bool isVTuber = tags.Any(DoesTagListContainAnyLabelsAsSubstrings(vtuberLabels));
            bool isLewdtuber = tags.Any(tag => tag.Contains("LEWDTUBER"));
            bool isSeiso = tags.Any(tag => tag.Contains("SEISO"));
            bool isArtist = tags.Any(DoesTagListPerfectlyMatchAnyLabels(exactArtistLabels)) && !tags.Any(tag => tag == "AIARIST");
            bool isMusician = tags.Any(DoesTagListContainAnyLabelsAsSubstrings(musicianLabels));
            bool isEighteenPlus = tags.Any(DoesTagListContainAnyLabelsAsSubstrings(eighteenPlusLabels));
            bool isNeurodivergent = tags.Any(DoesTagListContainAnyLabelsAsSubstrings(neurodivergentLabels));

            //adjectives
            if (isEighteenPlus)
            {
                interestingTagCommentary.Append("18+ ");
            }
            if (isCozy)
            {
                interestingTagCommentary.Append("cozy ");
            }

            if (isNeurodivergent)
            {
                interestingTagCommentary.Append("neurodivergent ");
            }

            if (isLGBTQ)
            {
                interestingTagCommentary.Append("queer ");
            }

            if (isTrans)
            {
                interestingTagCommentary.Append("trans ");
            }

            if (isSeiso)
            {
                interestingTagCommentary.Append("seiso ");
            }


            //nouns
            bool nounMissing = true;
            if (isArtist)
            {
                interestingTagCommentary.Append("artist ");
                nounMissing = false;
            }
            if (isMusician)
            {
                interestingTagCommentary.Append("musician ");
                nounMissing = false;
            }

            if (isLewdtuber)
            {
                interestingTagCommentary.Append("lewdtuber ");
                nounMissing = false;
            }
            else if (isVTuber)
            {
                interestingTagCommentary.Append("VTuber ");
                nounMissing = false;
            }


            if (interestingTagCommentary.Length != 0)
            {
                if (nounMissing)
                {
                    interestingTagCommentary.Append("streamer");
                }

                string interestingTagTerms = interestingTagCommentary.ToString().Trim();
                string aOrAn = vowels.Contains(interestingTagTerms[0]) ? "an" : "a";

                interestingTagCommentary = new StringBuilder($"  They're {aOrAn} {interestingTagTerms}!");
            }

            return interestingTagCommentary.ToString();
        }

        private static Func<string, bool> DoesTagListContainAnyLabelsAsSubstrings(IReadOnlyList<string> labelTags)
        {
            return tag => labelTags.Any(labelTag => tag.Contains(labelTag));
        }
        private static Func<string, bool> DoesTagListPerfectlyMatchAnyLabels(IReadOnlyList<string> labelTags)
        {
            return tag => labelTags.Any(labelTag => labelTag == tag);
        }

        private static bool DoesTagDescribeQueerStreamer(string tag)
        {
            tag = tag.ToUpper();

            bool hasLgbtqSubstring = tag.Contains("LGBTQ") && !tag.ToUpper().Contains("LGBTQFRIENDLY");
            bool hasSpecificQueerLabel = exactQueerLabels.Contains(tag);

            return hasLgbtqSubstring || hasSpecificQueerLabel;
        }

    }
}

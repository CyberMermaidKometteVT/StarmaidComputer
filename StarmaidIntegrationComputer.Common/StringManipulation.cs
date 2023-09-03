namespace StarmaidIntegrationComputer.Common
{
    public static class StringManipulation
    {
        public static string SanitizeForRichTextBox(string input)
        {
            //Note: 9 is tab, 10 is LF (newline), 13 is CR, half of the Windows newline
            IEnumerable<char> invalidChars = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 }
            .Select(invalidCharAsInt => (char)invalidCharAsInt);

            foreach (char invalidChar in invalidChars)
            {
                input = input.Replace($"{invalidChar}", "");
            }

            return input;
        }
    }
}

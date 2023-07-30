namespace StarmaidIntegrationComputer.Common.DataStructures
{
    public static class StringListExtensions
    {
        public static void ReplaceForAllStringsInList(this List<string> target, string valueToReplace, string newValue, int firstIndex = 0)
        {
            for (int targetIndex = firstIndex; targetIndex < target.Count; targetIndex++)
            {
                string value = target[targetIndex];
                if (value.Contains(valueToReplace))
                {
                    target[targetIndex] = value.Replace(valueToReplace, newValue);
                }
            }
        }
    }
}

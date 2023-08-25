namespace StarmaidIntegrationComputer.Common.DataStructures.StarmaidState
{
    public class RaiderInfoRaidTimeComparer : IComparer<RaiderInfo>
    {
        public int Compare(RaiderInfo? x, RaiderInfo? y)
        {
            const int Y_FIRST = 1;
            const int X_FIRST = -1;
            const int SAME = 0;

            if (x == null && y == null)
            {
                return SAME;
            }

            if (x == null && y != null)
            {
                return Y_FIRST;
            }

            if (x != null && y == null)
            {
                return X_FIRST;
            }

            //Nothing is null, good!

            var nameCompareResult = string.Compare(x.RaiderName, y.RaiderName);
            if (nameCompareResult == SAME)
            {
                //Same raider name, let's just treat this as though it already exists in the set.  Which means we'll need to update it, not add it.
                return SAME;
            }

            var dateCompareResult = DateTime.Compare(x.RaidTime, y.RaidTime);

            //Different raid time
            if (dateCompareResult != 0)
            {
                return dateCompareResult;
            }

            //Different raider name, exact same raid time.
            return nameCompareResult;
        }
    }
}

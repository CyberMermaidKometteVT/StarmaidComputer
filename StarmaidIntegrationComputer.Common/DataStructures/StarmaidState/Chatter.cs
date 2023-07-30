namespace StarmaidIntegrationComputer.Common.DataStructures.StarmaidState
{
    public class Chatter
    {
        public string ChatterName { get; }
        public Stack<ChatterMessageInfo> RecentMessages { get; } = new Stack<ChatterMessageInfo>();
        public DateTime? MostRecentChatDate
        {
            get
            {
                if (RecentMessages.Count == 0)
                {
                    return null;
                }

                return RecentMessages.ElementAtOrDefault(RecentMessages.Count - 1)?.Timestamp;
            }
        }

        public Chatter(string chatterName, ChatterMessageInfo? message = null)
        {
            ChatterName = chatterName;
            if (message != null)
            {
                RecentMessages.Push(message);
            }
        }
    }
}

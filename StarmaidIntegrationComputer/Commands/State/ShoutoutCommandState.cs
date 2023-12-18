namespace StarmaidIntegrationComputer.Commands.State
{
    internal class ShoutoutCommandState
    {
        public bool IsValidUser { get; set; } = false;
        public bool IsLive { get; set; } = false;
        public string? RecipientBroadcasterId { get; set; }
        public string? LastCategoryName { get; set; }
        public string? LastTitle { get; internal set; }
        public string? InterestingTagCommentary { get; internal set; }
    }
}

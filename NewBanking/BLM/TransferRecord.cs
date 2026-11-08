namespace NewBanking
{
    public static class TransferStatuses
    {
        public const string Pending = "Pending";
        public const string Completed = "Completed";
        public const string Refunded = "Refunded";
        public const string Unconfirmed = "Unconfirmed";
    }

    // Stored with the sender's account so a balance change and transfer state
    // are written together in accounts.json.
    public class TransferRecord
    {
        public string TransferId { get; set; } = "";
        public string DestinationBank { get; set; } = "";
        public string DestinationAccount { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = TransferStatuses.Pending;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public string? ResolvedBy { get; set; }
        public string? Resolution { get; set; }
    }
}

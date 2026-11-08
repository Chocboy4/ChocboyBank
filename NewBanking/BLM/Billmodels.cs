using System;
using System.Collections.Generic;

namespace NewBanking.BLM
{
    // What TatumBank sends to ChocboyBank
    public class BillPurchaseRequest
    {
        public string Reference { get; set; } = "";     // unique per purchase; Chocboy uses it to avoid double-fulfilling
        public string Type { get; set; } = "";          // "airtime" or "data"
        public string Network { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public string? PlanCode { get; set; }           // only for data
    }

    // Comes from ChocboyBank's catalog
    public class DataPlan
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }

    // Internal ledger entity. NOT an Account: no PIN, no login, never in the customer list.
    public class BillSettlementEntry
    {
        public string Reference { get; set; } = "";
        public string Account { get; set; } = "";       // customer Name, so pending entries can be resolved later
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending | Success | Reversed
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class BillSettlement
    {
        public string Name { get; set; } = "ChocboyBank Bill Settlement";
        public decimal Balance { get; set; }
        public List<BillSettlementEntry> Entries { get; set; } = new();
    }
}
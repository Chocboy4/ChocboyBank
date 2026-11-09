using System;
using System.Collections.Generic;

namespace NewBanking.BLM
{
    
    public class BillPurchaseRequest
    {
        public string Reference { get; set; } = "";     
        public string Type { get; set; } = "";          
        public string Network { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public string? PlanCode { get; set; }       
    }

    
    public class DataPlan
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }

    
    public class BillSettlementEntry
    {
        public string Reference { get; set; } = "";
        public string Account { get; set; } = "";       
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class BillSettlement
    {
        public string Name { get; set; } = "ChocboyBank Bill Settlement";
        public decimal Balance { get; set; }
        public List<BillSettlementEntry> Entries { get; set; } = new();
    }
}
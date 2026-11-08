using System;
using System.Collections.Generic;

namespace NewBanking.BLM
{
    
    public class BillRecord
    {
        public string Reference { get; set; } = "";
        public string Source { get; set; } = "Other bank";  
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime DateUtc { get; set; } = DateTime.UtcNow;
    }

    
    public class BillLedger
    {
        
        public decimal Receivable { get; set; }

        
        public decimal OwnSales { get; set; }

        
        public Dictionary<string, BillRecord> Records { get; set; } = new();
    }
}
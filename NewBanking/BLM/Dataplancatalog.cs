using System;
using System.Collections.Generic;
using System.Linq;

namespace NewBanking.BLM
{
   
    public static class DataPlanCatalog
    {
        private static readonly string[] Networks =
            { "MTN", "Airtel", "Glo", "9mobile", "Etisalat" };

        public static readonly List<DataPlan> Plans = new()
        {
            new DataPlan { Code = "DATA-100MB", Name = "100MB - 1 day",   Price = 100m },
            new DataPlan { Code = "DATA-500MB", Name = "500MB - 7 days",  Price = 500m },
            new DataPlan { Code = "DATA-1GB",   Name = "1GB - 30 days",   Price = 1000m },
            new DataPlan { Code = "DATA-2GB",   Name = "2GB - 30 days",   Price = 1500m },
            new DataPlan { Code = "DATA-5GB",   Name = "5GB - 30 days",   Price = 3500m }
        };

        public static bool IsValidNetwork(string? network) =>
            Networks.Any(n => n.Equals(network, StringComparison.OrdinalIgnoreCase));

        public static DataPlan? Find(string? code) =>
            Plans.FirstOrDefault(p => p.Code == code);
    }
}
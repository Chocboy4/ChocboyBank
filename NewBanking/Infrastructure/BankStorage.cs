using NewBanking.BLM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace NewBanking.Infrastructure
{
    public static class BankStorage
    {
        
        public static readonly object SyncRoot = new();


        private static readonly string FileName =
            Path.Combine(AppContext.BaseDirectory, "accounts.json");

        private static readonly string BillFileName =
            Path.Combine(AppContext.BaseDirectory, "bills.json");

        public static List<Account> LoadAccounts()
        {
            if (!File.Exists(FileName))
            {
                return new List<Account>();
            }

            try
            {
                string json = File.ReadAllText(FileName);

                return JsonSerializer.Deserialize<List<Account>>(json)
                       ?? new List<Account>();
            }
            catch (JsonException)
            {
                Console.WriteLine(
                    "Error: accounts.json contains invalid JSON."
                );

                throw;
            }
        }

        public static BillLedger LoadBillLedger()
        {
            if (!File.Exists(BillFileName))
            {
                return new BillLedger();
            }

            try
            {
                string json = File.ReadAllText(BillFileName);

                return JsonSerializer.Deserialize<BillLedger>(json)
                       ?? new BillLedger();
            }
            catch (JsonException)
            {
                Console.WriteLine("Error: bills.json contains invalid JSON.");
                throw;
            }
        }

        public static void SaveAccounts(List<Account> accounts)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            lock (SyncRoot)
            {
                string json = JsonSerializer.Serialize(accounts, options);

                File.WriteAllText(FileName, json);
            }
        }

        public static void SaveBillLedger(BillLedger ledger)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            lock (SyncRoot)
            {
                string json = JsonSerializer.Serialize(ledger, options);

                File.WriteAllText(BillFileName, json);
            }
        }
    }

}
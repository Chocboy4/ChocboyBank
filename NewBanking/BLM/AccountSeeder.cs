using NewBanking.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking.BLM
{
    public class AccountSeeder
    {
        private const string AdminName = "admin";
        private const string AdminDefaultPin = "4321";

        private static readonly Dictionary<string, string> DefaultPins = new()
        {
            ["francis"] = "1234",
            ["Richard"] = "5678",
            ["brian"] = "9012",
            ["Peter"] = "3456"
        };

        public static void Seed(List<Account> accounts)
        {
            bool updated = false;
            foreach (var acc in accounts)
            {
                if (string.IsNullOrEmpty(acc.Pin))
                {
                    acc.Pin = DefaultPins.GetValueOrDefault(acc.Name.ToLower(), "0000");
                    updated = true;
                }
            }

            if (accounts.Count == 0)
            {
                accounts.AddRange(new[]
                {
                    new Account { Name = "francis", Pin = "1234", Balance = 100000m },
                    new Account { Name = "Richard", Pin = "5678", Balance = 5000000m },
                    new Account { Name = "brian", Pin = "9012", Balance = 250000m },
                    new Account { Name = "Peter", Pin = "3456", Balance = 7500000m }
                });

                updated = true;
            }

            if (EnsureAdmin(accounts))
            {
                updated = true;
            }

            if (updated)
            {
                BankStorage.SaveAccounts(accounts);
            }
        }

        
        private static bool EnsureAdmin(List<Account> accounts)
        {
            if (accounts.Any(a => a.IsAdmin))
            {
                return false;
            }

            if (accounts.Any(a => a.Name.Equals(AdminName, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine(
                    $"Warning: a customer account named '{AdminName}' already exists, so no admin was created.");
                return false;
            }

            accounts.Add(new Account
            {
                Name = AdminName,
                Pin = AdminDefaultPin,
                Balance = 0m,
                IsAdmin = true
            });

            return true;
        }
    }
}
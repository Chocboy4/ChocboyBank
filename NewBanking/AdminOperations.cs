using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking
{
    public class AdminOperations
    {
        private const int MaxPinAttempts = 3;

        private readonly List<Account> _accounts;

        public AdminOperations(List<Account> accounts)
        {
            _accounts = accounts;
        }

        public void ListAccounts()
        {
            Console.WriteLine("\n{0,-15}{1,20}  {2}", "Name", "Balance", "Status");
            Console.WriteLine(new string('-', 45));

            foreach (var acc in _accounts.Where(a => !a.IsAdmin))
            {
                string status = acc.IsFrozen ? "FROZEN" : "Active";
                Console.WriteLine("{0,-15}{1,20}  {2}", acc.Name, $"₦{acc.Balance:N2}", status);
            }

            ConsoleHelper.Pause();
        }

        
        public bool Freeze(Account admin) => SetFrozen(admin, freeze: true);

        public bool Unfreeze(Account admin) => SetFrozen(admin, freeze: false);

        private bool SetFrozen(Account admin, bool freeze)
        {
            string action = freeze ? "freeze" : "unfreeze";

            Console.Write($"Enter the name of the account to {action}: ");
            string name = ConsoleHelper.ReadTrimmed();

            Account? target = FindCustomer(name);

            if (target == null)
            {
                Console.WriteLine("Customer account not found.");
                ConsoleHelper.Pause();
                return true;
            }

            if (target.IsFrozen == freeze)
            {
                Console.WriteLine($"{target.Name}'s account is already {(freeze ? "frozen" : "active")}.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.WriteLine($"You are about to {action} {target.Name}'s account.");

            if (!ConsoleHelper.VerifyPin(admin, MaxPinAttempts))
            {
                Console.WriteLine("Too many incorrect attempts. You have been logged out.");
                ConsoleHelper.Pause();
                return false;
            }

            string logEntry = freeze
                ? "Account frozen by admin. Sending money is disabled."
                : "Account unfrozen by admin.";

            target.IsFrozen = freeze;
            target.Transactions.Add(logEntry);

            try
            {
                BankStorage.SaveAccounts(_accounts);
                Console.WriteLine($"\n{target.Name}'s account has been {(freeze ? "frozen" : "unfrozen")}.");
            }
            catch (Exception ex)
            {
                
                target.IsFrozen = !freeze;
                target.Transactions.RemoveAt(target.Transactions.Count - 1);

                Console.WriteLine($"Could not save the change: {ex.Message}");
                Console.WriteLine("No changes were made.");
            }

            ConsoleHelper.Pause();
            return true;
        }

        public void ViewTransactions()
        {
            Console.Write("Enter the name of the account to view: ");
            string name = ConsoleHelper.ReadTrimmed();

            Account? target = FindCustomer(name);

            if (target == null)
            {
                Console.WriteLine("Customer account not found.");
                ConsoleHelper.Pause();
                return;
            }

            Console.WriteLine($"\n{target.Name}'s Transaction History ({(target.IsFrozen ? "FROZEN" : "Active")})");

            if (target.Transactions.Count == 0)
            {
                Console.WriteLine("No transactions found.");
            }
            else
            {
                for (int i = 0; i < target.Transactions.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {target.Transactions[i]}");
                }
            }

            ConsoleHelper.Pause();
        }

        
        public bool ChangePin(Account admin)
        {
            return PinChanger.Change(admin, _accounts);
        }

        private Account? FindCustomer(string name)
        {
            return _accounts.Find(a =>
                !a.IsAdmin && a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
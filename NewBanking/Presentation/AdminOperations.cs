using NewBanking.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking.Presentation
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
            Console.WriteLine("\n{0,-15}{1,-14}{2,20}  {3}", "Name", "Account Number", "Balance", "Status");
            Console.WriteLine(new string('-', 61));

            foreach (var acc in _accounts.Where(a => !a.IsAdmin))
            {
                string status = acc.IsFrozen ? "FROZEN" : "Active";
                Console.WriteLine("{0,-15}{1,-14}{2,20}  {3}", acc.Name, acc.AccountNumber, $"₦{acc.Balance:N2}", status);
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

        public bool DepositToCustomer(Account admin)
        {
            Console.Write("Enter the customer's name or account number: ");
            string searchValue = ConsoleHelper.ReadTrimmed();
            Account? target = FindCustomer(searchValue);

            if (target == null)
            {
                Console.WriteLine("Customer account not found.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.Write("Enter amount to deposit: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount <= 0)
            {
                Console.WriteLine("Please enter a valid deposit amount greater than zero.");
                ConsoleHelper.Pause();
                return true;
            }

            if (decimal.Round(amount, 2) != amount)
            {
                Console.WriteLine("Amount cannot have more than 2 decimal places.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.WriteLine($"You are about to deposit ₦{amount:N2} into {target.Name}'s account.");
            if (!ConsoleHelper.VerifyPin(admin, MaxPinAttempts))
            {
                Console.WriteLine("Too many incorrect attempts. You have been logged out.");
                ConsoleHelper.Pause();
                return false;
            }

            lock (BankStorage.SyncRoot)
            {
                target.Balance += amount;
                target.Transactions.Add($"Admin deposit: ₦{amount:N2}");
                admin.Transactions.Add($"Deposited ₦{amount:N2} into {target.Name}'s account ({target.AccountNumber})");

                try
                {
                    BankStorage.SaveAccounts(_accounts);
                    Console.WriteLine($"\nDeposit successful! {target.Name}'s new balance is ₦{target.Balance:N2}.");
                }
                catch (Exception ex)
                {
                    target.Balance -= amount;
                    target.Transactions.RemoveAt(target.Transactions.Count - 1);
                    admin.Transactions.RemoveAt(admin.Transactions.Count - 1);
                    Console.WriteLine($"Could not save the deposit: {ex.Message}");
                    Console.WriteLine("No deposit was made.");
                }
            }

            ConsoleHelper.Pause();
            return true;
        }

        
        public bool ResolveUnconfirmedTransfers(Account admin)
        {
            var unresolved = _accounts
                .Where(a => !a.IsAdmin)
                .SelectMany(a => a.Transfers
                    .Where(t => t.Status == NewBanking.TransferStatuses.Unconfirmed)
                    .Select(t => (Sender: a, Transfer: t)))
                .ToList();

            if (unresolved.Count == 0)
            {
                Console.WriteLine("\nThere are no unconfirmed transfers to resolve.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.WriteLine("\nUnconfirmed transfers:");
            foreach (var item in unresolved)
            {
                var t = item.Transfer;
                Console.WriteLine($"{t.TransferId} | {item.Sender.Name} | ₦{t.Amount:N2} | {t.DestinationAccount} at {t.DestinationBank} | {t.CreatedAtUtc:u}");
            }

            Console.Write("\nEnter the full transfer reference to resolve (or press Enter to cancel): ");
            string transferId = ConsoleHelper.ReadTrimmed();
            if (string.IsNullOrWhiteSpace(transferId)) return true;

            var selected = unresolved.FirstOrDefault(x =>
                x.Transfer.TransferId.Equals(transferId, StringComparison.OrdinalIgnoreCase));
            if (selected.Transfer == null)
            {
                Console.WriteLine("No unconfirmed transfer matches that full reference.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.WriteLine("\n1. Confirm delivered (do not refund)");
            Console.WriteLine("2. Refund sender");
            Console.WriteLine("3. Cancel");
            Console.Write("Choose a resolution: ");
            string choice = ConsoleHelper.ReadTrimmed();
            if (choice == "3") return true;
            if (choice is not ("1" or "2"))
            {
                Console.WriteLine("Invalid resolution.");
                ConsoleHelper.Pause();
                return true;
            }

            string action = choice == "1" ? "confirm this transfer as delivered" : "refund the sender";
            Console.WriteLine($"You are about to {action}. This cannot be repeated.");
            if (!ConsoleHelper.VerifyPin(admin, MaxPinAttempts))
            {
                Console.WriteLine("Too many incorrect attempts. You have been logged out.");
                ConsoleHelper.Pause();
                return false;
            }

            lock (BankStorage.SyncRoot)
            {
                
                if (selected.Transfer.Status != NewBanking.TransferStatuses.Unconfirmed)
                {
                    Console.WriteLine("This transfer has already been resolved.");
                    ConsoleHelper.Pause();
                    return true;
                }

                decimal previousBalance = selected.Sender.Balance;
                int senderEntryCount = selected.Sender.Transactions.Count;
                int adminEntryCount = admin.Transactions.Count;

                if (choice == "1")
                {
                    selected.Transfer.Status = NewBanking.TransferStatuses.Completed;
                    selected.Transfer.Resolution = "Confirmed delivered by admin.";
                    selected.Sender.Transactions.Add($"Transfer confirmed delivered by admin (ref {ShortRef(selected.Transfer.TransferId)}).");
                }
                else
                {
                    selected.Sender.Balance += selected.Transfer.Amount;
                    selected.Transfer.Status = NewBanking.TransferStatuses.Refunded;
                    selected.Transfer.Resolution = "Refunded by admin after manual review.";
                    selected.Sender.Transactions.Add($"Admin refund: ₦{selected.Transfer.Amount:N2} for unresolved transfer (ref {ShortRef(selected.Transfer.TransferId)}).");
                }

                selected.Transfer.ResolvedAtUtc = DateTime.UtcNow;
                selected.Transfer.ResolvedBy = admin.Name;
                admin.Transactions.Add($"Resolved transfer {ShortRef(selected.Transfer.TransferId)} for {selected.Sender.Name}: {selected.Transfer.Resolution}");

                try
                {
                    BankStorage.SaveAccounts(_accounts);
                    Console.WriteLine(choice == "1"
                        ? "\nTransfer marked as completed."
                        : $"\nSender refunded ₦{selected.Transfer.Amount:N2}.");
                }
                catch (Exception ex)
                {
                    selected.Sender.Balance = previousBalance;
                    selected.Sender.Transactions.RemoveRange(senderEntryCount, selected.Sender.Transactions.Count - senderEntryCount);
                    admin.Transactions.RemoveRange(adminEntryCount, admin.Transactions.Count - adminEntryCount);
                    selected.Transfer.Status = NewBanking.TransferStatuses.Unconfirmed;
                    selected.Transfer.ResolvedAtUtc = null;
                    selected.Transfer.ResolvedBy = null;
                    selected.Transfer.Resolution = null;
                    Console.WriteLine($"Could not save the resolution: {ex.Message}");
                    Console.WriteLine("No resolution was applied.");
                }
            }

            ConsoleHelper.Pause();
            return true;
        }

        public bool ChangePin(Account admin)
        {
            return PinChanger.Change(admin, _accounts);
        }

        private Account? FindCustomer(string searchValue)
        {
            return _accounts.Find(a =>
                !a.IsAdmin &&
                (a.Name.Equals(searchValue, StringComparison.OrdinalIgnoreCase) ||
                 a.AccountNumber == searchValue));
        }

        private static string ShortRef(string id) => id.Length > 8 ? id[..8] : id;
    }
}

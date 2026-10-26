using NewBanking.Infrastructure;
using NewBanking.Presentation;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace NewBanking.BLM
{
    public class BankingOperations
    {
        private const int MaxPinAttempts = 3;
        private static readonly string[] ValidNetworks =
            { "MTN", "Airtel", "Glo", "9mobile", "Etisalat" };

        private readonly List<Account> _accounts;

        public BankingOperations(List<Account> accounts)
        {
            _accounts = accounts;
        }

        public void CheckBalance(Account current)
        {
            Console.WriteLine($"\nYour balance is: ₦{current.Balance:N2}");

            if (current.IsFrozen)
            {
                Console.WriteLine("Account status: FROZEN (you can receive money but not send it).");
            }

            ConsoleHelper.Pause();
        }

        
        private static bool BlockIfFrozen(Account current)
        {
            if (!current.IsFrozen)
            {
                return false;
            }

            Console.WriteLine("\nYour account is frozen. You cannot send money or buy airtime.");
            Console.WriteLine("You can still receive money. Please contact the bank for help.");
            ConsoleHelper.Pause();
            return true;
        }

        public bool Transfer(Account current)
        {
            if (BlockIfFrozen(current))
            {
                return true;
            }

            Console.WriteLine("\n1. Same bank");
            Console.WriteLine("2. Another bank");
            Console.Write("Choose transfer type: ");

            string type = ConsoleHelper.ReadTrimmed();

            if (type == "1") return TransferLocal(current);
            if (type == "2") return TransferToOtherBank(current);

            Console.WriteLine("Invalid option.");
            ConsoleHelper.Pause();
            return true;
        }

        private bool TransferLocal(Account current)
        {
            Console.Write("Enter the recipient's account name: ");
            string transferTo = ConsoleHelper.ReadTrimmed();

            
            Account? recipient = _accounts.Find(
                a => !a.IsAdmin && a.Name.Equals(transferTo, StringComparison.OrdinalIgnoreCase));

            if (recipient == null)
            {
                Console.WriteLine("Recipient account not found.");
                ConsoleHelper.Pause();
                return true;
            }

            if (recipient == current)
            {
                Console.WriteLine("You cannot transfer money to yourself.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.Write("Enter amount to transfer: ");

            if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                Console.WriteLine("Please enter a valid amount.");
                ConsoleHelper.Pause();
                return true;
            }

            if (amount <= 50)
            {
                Console.WriteLine("Amount must be greater than fifty naira.");
                ConsoleHelper.Pause();
                return true;
            }

            if (amount > current.Balance)
            {
                Console.WriteLine("Insufficient funds.");
                ConsoleHelper.Pause();
                return true;
            }

            if (!ConsoleHelper.VerifyPin(current, MaxPinAttempts))
            {
                Console.WriteLine("Too many incorrect attempts. You have been logged out.");
                ConsoleHelper.Pause();
                return false;
            }

            lock (BankStorage.SyncRoot)
            {
                current.Balance -= amount;
                recipient.Balance += amount;

                current.Transactions.Add($"Transferred ₦{amount:N2} to {recipient.Name}");
                recipient.Transactions.Add($"Received ₦{amount:N2} from {current.Name}");

                try
                {
                    BankStorage.SaveAccounts(_accounts);

                    Console.WriteLine(
                        $"\nTransfer successful! You sent ₦{amount:N2} to {recipient.Name}.");
                    Console.WriteLine($"Your new balance is {current.Balance:N2}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not save the transfer: {ex.Message}");
                    Console.WriteLine("Restarting is not recommended until the data is checked.");
                }
            }

            ConsoleHelper.Pause();
            return true;
        }

        public bool BuyAirtime(Account current)
        {
            if (BlockIfFrozen(current))
            {
                return true;
            }

            Console.Write("Choose network (MTN, Airtel, Glo, 9mobile): ");
            string network = ConsoleHelper.ReadTrimmed();

            bool validNetwork = Array.Exists(
                ValidNetworks,
                n => n.Equals(network, StringComparison.OrdinalIgnoreCase));

            if (!validNetwork)
            {
                Console.WriteLine("Invalid network.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.Write("Enter phone number: ");
            string phoneNumber = ConsoleHelper.ReadTrimmed();

            if (phoneNumber.Length != 11 ||
                !phoneNumber.StartsWith("0") ||
                !phoneNumber.All(char.IsDigit))
            {
                Console.WriteLine("Enter a valid 11-digit Nigerian phone number.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.Write("Enter airtime amount: ");

            if (!decimal.TryParse(Console.ReadLine(), out decimal airtimeAmount))
            {
                Console.WriteLine("Please enter a valid amount.");
                ConsoleHelper.Pause();
                return true;
            }

            if (airtimeAmount <= 50)
            {
                Console.WriteLine("Amount must be greater than Fourty nine Naira.");
                ConsoleHelper.Pause();
                return true;
            }

            if (!ConsoleHelper.VerifyPin(current, MaxPinAttempts))
            {
                Console.WriteLine("Too many incorrect attempts. You have been logged out.");
                ConsoleHelper.Pause();
                return false;
            }

            if (airtimeAmount > current.Balance)
            {
                Console.WriteLine("Insufficient funds.");
                ConsoleHelper.Pause();
                return true;
            }

            lock (BankStorage.SyncRoot)
            {
                current.Balance -= airtimeAmount;

                current.Transactions.Add(
                    $"Purchased airtime for {phoneNumber} on {network}, amount: ₦{airtimeAmount:N2}");

                try
                {
                    BankStorage.SaveAccounts(_accounts);

                    Console.WriteLine("\nAirtime purchase successful!");
                    Console.WriteLine($"Phone number: {phoneNumber}");
                    Console.WriteLine($"Amount: ₦{airtimeAmount:N2}");
                    Console.WriteLine($"New balance: ₦{current.Balance:N2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not save the airtime purchase: {ex.Message}");
                }
            }

            ConsoleHelper.Pause();
            return true;
        }

        private bool TransferToOtherBank(Account current)
        {
            if (!InterBankSettings.Enabled)
            {
                Console.WriteLine("\nOther-bank transfers are not set up.");
                Console.WriteLine("Start the app with: dotnet run -- <your-port> <other-bank-url>");
                ConsoleHelper.Pause();
                return true;
            }

            Console.Write($"Enter the recipient's account name at {InterBankSettings.PeerLabel}: ");
            string toAccount = ConsoleHelper.ReadTrimmed();

            if (string.IsNullOrWhiteSpace(toAccount))
            {
                Console.WriteLine("Account name cannot be empty.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.Write("Enter amount to transfer: ");

            if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                Console.WriteLine("Please enter a valid amount.");
                ConsoleHelper.Pause();
                return true;
            }

            if (amount <= 50)
            {
                Console.WriteLine("Amount must be greater than fifty naira.");
                ConsoleHelper.Pause();
                return true;
            }

            if (decimal.Round(amount, 2) != amount)
            {
                Console.WriteLine("Amount cannot have more than 2 decimal places.");
                ConsoleHelper.Pause();
                return true;
            }

            if (amount > current.Balance)
            {
                Console.WriteLine("Insufficient funds.");
                ConsoleHelper.Pause();
                return true;
            }

            if (!ConsoleHelper.VerifyPin(current, MaxPinAttempts))
            {
                Console.WriteLine("Too many incorrect attempts. You have been logged out.");
                ConsoleHelper.Pause();
                return false;
            }

            string transferId = Guid.NewGuid().ToString("N");
            string shortRef = transferId[..8];
            string destination = $"{toAccount} at {InterBankSettings.PeerLabel}";
            int entryIndex;

            
            lock (BankStorage.SyncRoot)
            {
                current.Balance -= amount;
                current.Transactions.Add($"PENDING: sending ₦{amount:N2} to {destination} (ref {shortRef})");
                entryIndex = current.Transactions.Count - 1;

                try
                {
                    BankStorage.SaveAccounts(_accounts);
                }
                catch (Exception ex)
                {
                    current.Balance += amount;
                    current.Transactions.RemoveAt(entryIndex);

                    Console.WriteLine($"Could not save, so the transfer was cancelled: {ex.Message}");
                    ConsoleHelper.Pause();
                    return true;
                }
            }

            
            Console.WriteLine("\nContacting the other bank...");

            var request = new TransferRequest(
                transferId, InterBankSettings.BankName, current.Name, toAccount, amount);

            SendResult result = InterBankClient.SendAsync(request).GetAwaiter().GetResult();

            
            lock (BankStorage.SyncRoot)
            {
                switch (result.Outcome)
                {
                    case SendOutcome.Delivered:
                        current.Transactions[entryIndex] =
                            $"Transferred ₦{amount:N2} to {destination} (ref {shortRef})";

                        Console.WriteLine($"\nTransfer successful! You sent ₦{amount:N2} to {destination}.");
                        Console.WriteLine($"Your new balance is ₦{current.Balance:N2}.");
                        break;

                    case SendOutcome.Rejected:
                    case SendOutcome.NotDelivered:
                        
                        current.Balance += amount;
                        current.Transactions[entryIndex] =
                            $"FAILED: ₦{amount:N2} to {destination} was not sent and was refunded (ref {shortRef})";

                        Console.WriteLine($"\nTransfer failed: {result.Message}");
                        Console.WriteLine("Your money has been returned to your account.");
                        break;

                    default:
                        
                        current.Transactions[entryIndex] =
                            $"UNCONFIRMED: ₦{amount:N2} to {destination}, the other bank did not confirm (ref {shortRef})";

                        Console.WriteLine($"\n{result.Message}");
                        Console.WriteLine("The other bank did not confirm the transfer.");
                        Console.WriteLine($"Your money is on hold, not lost. Do not retry. Quote ref {shortRef} to the bank.");
                        break;
                }

                try
                {
                    BankStorage.SaveAccounts(_accounts);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not save the result: {ex.Message}");
                    Console.WriteLine("Restarting is not recommended until the data is checked.");
                }
            }

            ConsoleHelper.Pause();
            return true;
        }

       
        public bool ChangePin(Account current)
        {
            return PinChanger.Change(current, _accounts);
        }

        public void ShowTransactions(Account current)
        {
            Console.WriteLine($"\n{current.Name}'s Transaction History");

            if (current.Transactions.Count == 0)
            {
                Console.WriteLine("No transactions found.");
            }
            else
            {
                for (int i = 0; i < current.Transactions.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {current.Transactions[i]}");
                }
            }

            ConsoleHelper.Pause();
        }
    }
}
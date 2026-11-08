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

        public bool Deposit(Account current)
        {
            Console.Write("Enter amount to deposit: ");

            if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount <= 500)
            {
                Console.WriteLine("Please enter a valid deposit from 500 and above.");
                ConsoleHelper.Pause();
                return true;
            }

            if (decimal.Round(amount, 2) != amount)
            {
                Console.WriteLine("Amount cannot have more than 2 decimal places.");
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
                current.Balance += amount;
                current.Transactions.Add($"Cash deposit: ₦{amount:N2}");

                try
                {
                    BankStorage.SaveAccounts(_accounts);
                    Console.WriteLine($"\nDeposit successful! Your new balance is ₦{current.Balance:N2}.");
                }
                catch (Exception ex)
                {
                    current.Balance -= amount;
                    current.Transactions.RemoveAt(current.Transactions.Count - 1);
                    Console.WriteLine($"Could not save the deposit: {ex.Message}");
                    Console.WriteLine("No deposit was made.");
                }
            }

            ConsoleHelper.Pause();
            return true;
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

            return ProcessOwnBill(current, "airtime", network, phoneNumber, airtimeAmount, null, "airtime");
        }

        public bool BuyData(Account current)
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

            // ChocboyBank sells the plans, so read its own list directly
            List<DataPlan> plans = DataPlanCatalog.Plans;

            for (int i = 0; i < plans.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {plans[i].Name} - ₦{plans[i].Price:N2}");
            }

            Console.Write("Select a plan: ");

            if (!int.TryParse(Console.ReadLine(), out int choice) ||
                choice < 1 || choice > plans.Count)
            {
                Console.WriteLine("Invalid selection.");
                ConsoleHelper.Pause();
                return true;
            }

            DataPlan plan = plans[choice - 1];

            if (!ConsoleHelper.VerifyPin(current, MaxPinAttempts))
            {
                Console.WriteLine("Too many incorrect attempts. You have been logged out.");
                ConsoleHelper.Pause();
                return false;
            }

            if (plan.Price > current.Balance)
            {
                Console.WriteLine("Insufficient funds.");
                ConsoleHelper.Pause();
                return true;
            }

            return ProcessOwnBill(current, "data", network, phoneNumber, plan.Price, plan.Code, $"data ({plan.Name})");
        }

        
        private bool ProcessOwnBill(
            Account current,
            string type,          
            string network,
            string phoneNumber,
            decimal amount,
            string? planCode,     
            string label)         
        {
            var request = new BillPurchaseRequest
            {
                Reference = "CB-" + Guid.NewGuid().ToString("N"),
                Type = type,
                Network = network,
                PhoneNumber = phoneNumber,
                Amount = amount,
                PlanCode = planCode
            };

            
            string? problem = BillProcessor.Check(request);

            if (problem != null)
            {
                Console.WriteLine($"\nPurchase failed: {problem}");
                Console.WriteLine("You have not been charged.");
                ConsoleHelper.Pause();
                return true;
            }

            lock (BankStorage.SyncRoot)
            {
                
                current.Balance -= amount;
                current.Transactions.Add(
                    $"Purchased {label} for {phoneNumber} on {network}, amount: ₦{amount:N2}");
                int entryIndex = current.Transactions.Count - 1;

                try
                {
                    BankStorage.SaveAccounts(_accounts);
                }
                catch (Exception ex)
                {
                    current.Balance += amount;
                    current.Transactions.RemoveAt(entryIndex);

                    Console.WriteLine($"Could not save, so the purchase was cancelled: {ex.Message}");
                    ConsoleHelper.Pause();
                    return true;
                }

                
                var (_, response) = BillProcessor.Process(request, ownCustomer: true);

                if (response.Success)
                {
                    Console.WriteLine($"\nPurchase successful!");
                    Console.WriteLine($"{label} for {phoneNumber} on {network}");
                    Console.WriteLine($"Amount: ₦{amount:N2}");
                    Console.WriteLine($"New balance: ₦{current.Balance:N2}");
                }
                else
                {
                    
                    current.Balance += amount;
                    current.Transactions[entryIndex] =
                        $"FAILED {label} for {phoneNumber} on {network}, ₦{amount:N2} refunded";

                    try
                    {
                        BankStorage.SaveAccounts(_accounts);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not save the refund: {ex.Message}");
                    }

                    Console.WriteLine($"\nPurchase failed: {response.Message}");
                    Console.WriteLine($"₦{amount:N2} has been refunded. Balance: ₦{current.Balance:N2}");
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
            var transfer = new TransferRecord
            {
                TransferId = transferId,
                DestinationBank = InterBankSettings.PeerLabel,
                DestinationAccount = toAccount,
                Amount = amount,
                Status = TransferStatuses.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            
            lock (BankStorage.SyncRoot)
            {
                current.Balance -= amount;
                current.Transactions.Add($"PENDING: sending ₦{amount:N2} to {destination} (ref {shortRef})");
                entryIndex = current.Transactions.Count - 1;
                current.Transfers.Add(transfer);

                try
                {
                    BankStorage.SaveAccounts(_accounts);
                }
                catch (Exception ex)
                {
                    current.Balance += amount;
                    current.Transactions.RemoveAt(entryIndex);
                    current.Transfers.Remove(transfer);

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
                        transfer.Status = TransferStatuses.Completed;

                        Console.WriteLine($"\nTransfer successful! You sent ₦{amount:N2} to {destination}.");
                        Console.WriteLine($"Your new balance is ₦{current.Balance:N2}.");
                        break;

                    case SendOutcome.Rejected:
                    case SendOutcome.NotDelivered:
                        
                        current.Balance += amount;
                        current.Transactions[entryIndex] =
                            $"FAILED: ₦{amount:N2} to {destination} was not sent and was refunded (ref {shortRef})";
                        transfer.Status = TransferStatuses.Refunded;
                        transfer.ResolvedAtUtc = DateTime.UtcNow;
                        transfer.Resolution = "Automatically refunded because the transfer was not delivered.";

                        Console.WriteLine($"\nTransfer failed: {result.Message}");
                        Console.WriteLine("Your money has been returned to your account.");
                        break;

                    default:
                        
                        current.Transactions[entryIndex] =
                            $"UNCONFIRMED: ₦{amount:N2} to {destination}, the other bank did not confirm (ref {shortRef})";
                        transfer.Status = TransferStatuses.Unconfirmed;

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

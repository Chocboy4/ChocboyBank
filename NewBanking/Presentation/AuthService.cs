using NewBanking.Infrastructure;
using NewBanking.BLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewBanking.Presentation
{
    public class AuthService
    {
        private const int MaxAttempts = 3;
        private const decimal MinOpeningDeposit = 2000m;
        private readonly List<Account> _accounts;

        public AuthService(List<Account> accounts)
        {
            _accounts = accounts;
        }

        public Account? Login()
        {
            int attempts = 0;
            Account? current = null;

            while (current == null)
            {
                Console.Write("Enter your account name or account number: ");
                string loginValue = ConsoleHelper.ReadTrimmed();
                if (string.IsNullOrWhiteSpace(loginValue))
                {
                    Console.WriteLine("Input cannot be empty. Please try again.\n");
                    continue;
                }

                current = _accounts.Find(
                    a => a.Name.Equals(loginValue, StringComparison.OrdinalIgnoreCase) ||
                         a.AccountNumber == loginValue);

                if (current != null)
                {
                    if (ConsoleHelper.VerifyPin(current, MaxAttempts))
                    {
                        return current;
                    }

                    Console.WriteLine("Too many incorrect PIN attempts. Thank you for using ChocboyBank. Goodbye!");
                    return null;
                }

                Console.WriteLine("\nAccount not found, Please check the name and try again.");
                attempts++;

                if (attempts >= MaxAttempts)
                {
                    Console.WriteLine("Too many incorrect attempts. Thank you for using ChocboyBank. Goodbye!");
                    return null;
                }

                Console.WriteLine($"Attempts remaining: {MaxAttempts - attempts}");
                Console.WriteLine("1. Try again");
                Console.WriteLine("2. Create a new account");
                Console.Write("Choose an option: ");

                string option = ConsoleHelper.ReadTrimmed();

                if (option == "2")
                {
                    current = CreateAccount();
                }
                else if (option != "1")
                {
                    Console.WriteLine("Invalid option. Try again.\n");
                }
                else
                {
                    Console.WriteLine();
                }
            }

            return current;
        }

        private Account? CreateAccount()
        {
            Console.Write("Enter a name for your new account: ");
            string newName = ConsoleHelper.ReadTrimmed().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(newName))
            {
                Console.WriteLine("Name cannot be empty.\n");
                return null;
            }

            if (_accounts.Exists(a => a.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("That account name already exists.\n");
                return null;
            }

            Console.Write("Choose a 4-digit PIN: ");
            string newPin = ConsoleHelper.ReadPin();

            if (newPin.Length != 4 || !newPin.All(char.IsDigit))
            {
                Console.WriteLine("PIN must be exactly 4 digits.\n");
                return null;
            }

            decimal openingDeposit = ReadOpeningDeposit();

            if (openingDeposit < MinOpeningDeposit)
            {
                Console.WriteLine("Account creation cancelled.\n");
                return null;
            }

            var account = new Account
            {
                Name = newName,
                AccountNumber = AccountNumberGenerator.CreateUnique(_accounts),
                Pin = newPin,
                Balance = openingDeposit
            };

            account.Transactions.Add($"You Created the Account with an initial deposit of ₦{openingDeposit:N2}");

            lock (BankStorage.SyncRoot)
            {
                _accounts.Add(account);

                try
                {
                    BankStorage.SaveAccounts(_accounts);
                }
                catch (Exception ex)
                {
                    _accounts.Remove(account);
                    Console.WriteLine($"Could not create the account: {ex.Message}\n");
                    return null;
                }
            }

            Console.WriteLine($"Account created for {account.Name}.");
            Console.WriteLine($"Your account number is: {account.AccountNumber}\n");
            return account;
        }

        private decimal ReadOpeningDeposit()
        {
            while (true)
            {
                Console.Write($"Enter opening deposit (minimum ₦{MinOpeningDeposit:N0}, or 0 to cancel): ");
                string input = ConsoleHelper.ReadTrimmed();

                if (!decimal.TryParse(input, out decimal amount) || amount < 0)
                {
                    Console.WriteLine("Please enter a valid amount.\n");
                    continue;
                }

                if (amount == 0)
                {
                    return 0m;
                }

                if (decimal.Round(amount, 2) != amount)
                {
                    Console.WriteLine("Amount cannot have more than 2 decimal places.\n");
                    continue;
                }

                if (amount < MinOpeningDeposit)
                {
                    Console.WriteLine($"The minimum opening deposit is ₦{MinOpeningDeposit:N0}.\n");
                    continue;
                }

                return amount;
            }
        }
    }
}
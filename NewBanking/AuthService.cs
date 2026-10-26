using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking
{
    public class AuthService
    {
        private const int MaxAttempts = 3;
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
                Console.Write("Enter the name on your account: ");
                string userName = ConsoleHelper.ReadTrimmed();

                current = _accounts.Find(
                    a => a.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));

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
            string newPin = ConsoleHelper.ReadTrimmed();

            if (newPin.Length != 4 || !newPin.All(char.IsDigit))
            {
                Console.WriteLine("PIN must be exactly 4 digits.\n");
                return null;
            }

            var account = new Account
            {
                Name = newName,
                Pin = newPin,
                Balance = 0m
            };

            _accounts.Add(account);
            BankStorage.SaveAccounts(_accounts);

            Console.WriteLine($"Account created for {account.Name}.\n");
            return account;
        }
    }
}

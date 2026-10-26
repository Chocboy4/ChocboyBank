using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking
{
    public static class PinChanger
    {
        private const int MaxPinAttempts = 3;

        
        public static bool Change(Account account, List<Account> accounts)
        {
            Console.WriteLine("\nChange PIN");
            Console.WriteLine("First, confirm your current PIN.");

            if (!ConsoleHelper.VerifyPin(account, MaxPinAttempts))
            {
                Console.WriteLine("Too many incorrect attempts. You have been logged out.");
                ConsoleHelper.Pause();
                return false;
            }

            Console.Write("Enter your new 4-digit PIN: ");
            string newPin = ConsoleHelper.ReadTrimmed();

            if (!IsValidPin(newPin))
            {
                Console.WriteLine("PIN must be exactly 4 digits. Your PIN was not changed.");
                ConsoleHelper.Pause();
                return true;
            }

            if (newPin == account.Pin)
            {
                Console.WriteLine("Your new PIN must be different from your current PIN.");
                ConsoleHelper.Pause();
                return true;
            }

            Console.Write("Confirm your new PIN: ");
            string confirmPin = ConsoleHelper.ReadTrimmed();

            if (confirmPin != newPin)
            {
                Console.WriteLine("The PINs do not match. Your PIN was not changed.");
                ConsoleHelper.Pause();
                return true;
            }

            string oldPin = account.Pin;

            account.Pin = newPin;
            account.Transactions.Add("PIN changed");

            try
            {
                BankStorage.SaveAccounts(accounts);
                Console.WriteLine("\nYour PIN has been changed successfully.");
            }
            catch (Exception ex)
            {
                
                account.Pin = oldPin;
                account.Transactions.RemoveAt(account.Transactions.Count - 1);

                Console.WriteLine($"Could not save the new PIN: {ex.Message}");
                Console.WriteLine("Your PIN was not changed.");
            }

            ConsoleHelper.Pause();
            return true;
        }

        private static bool IsValidPin(string pin)
        {
            return pin.Length == 4 && pin.All(c => c >= '0' && c <= '9');
        }
    }
}
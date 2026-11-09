using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking.Presentation
{
    public class ConsoleHelper
    {
        public static void Pause()
        {
            Console.WriteLine("\nPress Enter to continue...");
            Console.ReadLine();
        }

        public static string ReadTrimmed()
        {
            return (Console.ReadLine() ?? "").Trim();
        }


        public static bool VerifyPin(Account account, int maxAttempts = 3)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Console.Write("Enter your PIN: ");
                string enteredPin = ReadPin();

                if (enteredPin == account.Pin)
                {
                    return true;
                }

                int attemptsLeft = maxAttempts - attempt;
                if (attemptsLeft > 0)
                {
                    ConsoleUI.Warning($"Incorrect PIN. {attemptsLeft} attempt(s) left.");
                }
            }

            return false;
        }

        public static string ReadPin()
        {
            var sb = new StringBuilder();
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
                {
                    sb.Length--;
                    Console.Write("\b \b");
                }
                else if (char.IsDigit(key.KeyChar))
                {
                    sb.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
            Console.WriteLine();
            return sb.ToString();
        }
    }
}

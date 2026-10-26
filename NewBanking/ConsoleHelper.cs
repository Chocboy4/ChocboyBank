using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking
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
                string enteredPin = ReadTrimmed();

                if (enteredPin == account.Pin)
                {
                    return true;
                }

                int attemptsLeft = maxAttempts - attempt;
                if (attemptsLeft > 0)
                {
                    Console.WriteLine($"Incorrect PIN. {attemptsLeft} attempt(s) left.");
                }
            }

            return false;
        }
    }
}

using NewBanking.BLM;
using NewBanking.Infrastructure;
using NewBanking.Presentation.Banners;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking.Presentation
{
    public class BankApp
    {
        public void Run()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            List<Account> accounts;

            try
            {
                accounts = BankStorage.LoadAccounts();
            }
            catch (Exception ex) when (
                ex is System.Text.Json.JsonException ||
                ex is System.IO.IOException ||
                ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"Unable to load accounts: {ex.Message}");
                return;
            }

            AccountSeeder.Seed(accounts);

            InterBankServer.Start(accounts);

            var auth = new AuthService(accounts);
            var operations = new BankingOperations(accounts);
            var adminOperations = new AdminOperations(accounts);

            while (true)
            {
                Account? current = auth.Login();
                if (current == null)
                {
                    return;
                }

                bool exitApp;

                if (current.IsAdmin)
                {
                    exitApp = RunAdminSession(current, adminOperations);
                }
                else
                {
                    string branch = BranchSelector.Prompt();
                    exitApp = RunSession(current, branch, operations);
                }

                if (exitApp)
                {
                    return;
                }
            }
        }

        private static bool RunSession(Account current, string branch, BankingOperations operations)
        {
            while (true)
            {
                Banner.Display($"Welcome to ChocboyBank {branch} branch, {current.Name}");

                

                if (current.IsFrozen)
                {
                    Console.WriteLine("\n*** Your account is frozen. You can receive money but cannot send it. ***");
                }

                Console.WriteLine("\n1. Check balance");
                Console.WriteLine("2. Transfer");
                Console.WriteLine("3. Airtime");
                Console.WriteLine("4. Transactions");
                Console.WriteLine("5. Change PIN");
                Console.WriteLine("6. Logout");
                Console.WriteLine("7. Exit");
                Console.Write("\nChoose an option: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        operations.CheckBalance(current);
                        break;

                    case "2":
                        if (!operations.Transfer(current)) return false;
                        break;

                    case "3":
                        if (!operations.BuyAirtime(current)) return false;
                        break;

                    case "4":
                        operations.ShowTransactions(current);
                        break;

                    case "5":
                        if (!operations.ChangePin(current)) return false;
                        break;

                    case "6":
                        Console.WriteLine($"\nGoodbye, {current.Name}! You have been logged out.");
                        return false;

                    case "7":
                        Console.WriteLine("Thank you for using ChocboyBank!");
                        return true;

                    default:
                        Console.WriteLine("Invalid option. Please choose 1 to 7.");
                        ConsoleHelper.Pause();
                        break;
                }
            }
        }

        private static bool RunAdminSession(Account admin, AdminOperations adminOperations)
        {
            while (true)
            {
                Banner.Display("ChocboyBank Admin");

                Console.WriteLine($"\nAdmin session: {admin.Name}");

                Console.WriteLine("\n1. View all accounts");
                Console.WriteLine("2. Freeze an account");
                Console.WriteLine("3. Unfreeze an account");
                Console.WriteLine("4. View an account's transactions");
                Console.WriteLine("5. Change PIN");
                Console.WriteLine("6. Logout");
                Console.WriteLine("7. Exit");
                Console.Write("\nChoose an option: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        adminOperations.ListAccounts();
                        break;

                    case "2":
                        if (!adminOperations.Freeze(admin)) return false;
                        break;

                    case "3":
                        if (!adminOperations.Unfreeze(admin)) return false;
                        break;

                    case "4":
                        adminOperations.ViewTransactions();
                        break;

                    case "5":
                        if (!adminOperations.ChangePin(admin)) return false;
                        break;

                    case "6":
                        Console.WriteLine($"\nGoodbye, {admin.Name}! You have been logged out.");
                        return false;

                    case "7":
                        Console.WriteLine("Thank you for using ChocboyBank!");
                        return true;

                    default:
                        Console.WriteLine("Invalid option. Please choose 1 to 7.");
                        ConsoleHelper.Pause();
                        break;
                }
            }
        }
    }
}
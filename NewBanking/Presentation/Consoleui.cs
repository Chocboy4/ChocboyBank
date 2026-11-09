using System;
using System.Text;
using System.Threading;

namespace NewBanking.Presentation
{
    public static class ConsoleUI
    {
        private const int Width = 52;

        
        public static void Init()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "ChocboyBank";
        }

      

        public static void Banner(string title, string? subtitle = null)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔" + new string('═', Width) + "╗");
            Console.WriteLine("║" + Center(title, Width) + "║");

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("║" + Center(subtitle, Width) + "║");
                Console.ForegroundColor = ConsoleColor.Cyan;
            }

            Console.WriteLine("╚" + new string('═', Width) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void Menu(params string[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"  [{i + 1}] ");
                Console.ResetColor();
                Console.WriteLine(items[i]);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  " + new string('─', Width - 2));
            Console.ResetColor();
        }

        public static void Prompt(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  ❯ {text}: ");
            Console.ResetColor();
        }

        public static void Section(string title)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ── {title} " + new string('─', Math.Max(0, Width - title.Length - 6)));
            Console.ResetColor();
        }

       

        public static void BalanceCard(string accountName, decimal balance)
        {
            string amount = $"₦{balance:N2}";

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  ┌" + new string('─', Width - 4) + "┐");
            Console.WriteLine("  │" + Left($" Account: {accountName}", Width - 4) + "│");
            Console.WriteLine("  │" + new string(' ', Width - 4) + "│");
            Console.Write("  │");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(Center(amount, Width - 4));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("│");
            Console.WriteLine("  └" + new string('─', Width - 4) + "┘");
            Console.ResetColor();
        }

        public static void Receipt(string title, params (string Label, string Value)[] rows)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ┌" + new string('─', Width - 4) + "┐");
            Console.WriteLine("  │" + Center($"✔ {title}", Width - 4) + "│");
            Console.WriteLine("  ├" + new string('─', Width - 4) + "┤");

            foreach (var (label, value) in rows)
            {
                string line = $" {label,-14}{value}";
                Console.WriteLine("  │" + Left(line, Width - 4) + "│");
            }

            Console.WriteLine("  └" + new string('─', Width - 4) + "┘");
            Console.ResetColor();
        }

        

        public static void Success(string message) => Write("✔ ", message, ConsoleColor.Green);
        public static void Error(string message) => Write("✘ ", message, ConsoleColor.Red);
        public static void Warning(string message) => Write("! ", message, ConsoleColor.Yellow);
        public static void Info(string message) => Write("• ", message, ConsoleColor.Cyan);

        private static void Write(string icon, string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("  " + icon + message);
            Console.ResetColor();
        }

        

        public static void Loading(string message, int steps = 6)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  {message}");

            for (int i = 0; i < steps; i++)
            {
                Thread.Sleep(150);
                Console.Write(".");
            }

            Console.WriteLine();
            Console.ResetColor();
        }

        public static void Pause()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Press Enter to continue...");
            Console.ResetColor();
            Console.ReadLine();
        }

        

        private static string Center(string text, int width)
        {
            if (text.Length >= width) return text[..width];
            int left = (width - text.Length) / 2;
            return new string(' ', left) + text + new string(' ', width - text.Length - left);
        }

        private static string Left(string text, int width)
        {
            return text.Length >= width ? text[..width] : text.PadRight(width);
        }
    }
}
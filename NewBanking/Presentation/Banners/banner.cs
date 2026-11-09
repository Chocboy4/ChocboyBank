using System;

namespace NewBanking.Presentation.Banners
{
    internal class Banner
    {
        public static void Display(string text, int pad = 2, int minWidth = 0,
                                   ConsoleColor color = ConsoleColor.Yellow)
        {
            
            int inner = Math.Max(text.Length + pad * 2, minWidth);

            
            int totalSpace = inner - text.Length;
            int left = totalSpace / 2;
            int right = totalSpace - left;

            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;

            Console.WriteLine("╔" + new string('═', inner) + "╗");
            Console.WriteLine("║" + new string(' ', left) + text + new string(' ', right) + "║");
            Console.WriteLine("╚" + new string('═', inner) + "╝");

            Console.ForegroundColor = original;
        }
    }
}

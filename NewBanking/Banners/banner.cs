
using System;

namespace NewBanking.Banners
{
    internal class Banner
    {
        public static void Display(string text, int pad = 1)
        {
            string line = new string('=', text.Length + pad * 2);

            Console.WriteLine(line);
            Console.WriteLine("=" + new string(' ', pad) + text
                + new string(' ', pad) + "=");
            Console.WriteLine(line);
        }
    }
}

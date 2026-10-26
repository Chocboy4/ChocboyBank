using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NewBanking.Infrastructure
{
    public static class InterBankSettings
    {
       
        public const string ApiKey = "ChocboyBank";

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static bool Enabled { get; private set; }
        public static int Port { get; private set; }
        public static string PeerUrl { get; private set; } = "";
        public static string BankName { get; private set; } = "ChocboyBank";
        public static string PeerName { get; private set; } = "";


        public static string PeerLabel =>!Enabled ? "" : PeerName != "" ? PeerName : new Uri(PeerUrl).Authority;


        public static void Configure(string[] args)
        {
            if (args.Length < 2)
            {
                return; 
            }

            if (!int.TryParse(args[0], out int port) || port is < 1024 or > 65535)
            {
                Console.WriteLine("Invalid port. Use a number between 1024 and 65535. Other-bank transfers are off.");
                return;
            }

            if (!Uri.TryCreate(args[1], UriKind.Absolute, out Uri? peer) ||
                (peer.Scheme != Uri.UriSchemeHttp && peer.Scheme != Uri.UriSchemeHttps))
            {
                Console.WriteLine("Invalid other-bank URL. Example: http://localhost:5002. Other-bank transfers are off.");
                return;
            }

            if (peer.IsLoopback && peer.Port == port)
            {
                Console.WriteLine("The other-bank URL points to this same app. Other-bank transfers are off.");
                return;
            }

            Port = port;
            PeerUrl = peer.GetLeftPart(UriPartial.Authority);
            BankName = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])
                ? args[2].Trim()
                : $"ChocboyBank-{port}";
            PeerName = args.Length > 3 ? args[3] : "";
            Enabled = true;
        }
    }
}
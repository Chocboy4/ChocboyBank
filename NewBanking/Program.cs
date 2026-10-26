using NewBanking.Infrastructure;
using NewBanking.Presentation;
using NewBanking.Presentation.Banners;


InterBankSettings.Configure(args);
Console.WriteLine("=================================================================");
Console.WriteLine($"Welcome to ChocboyBank");
Console.WriteLine("=================================================================");

new BankApp().Run();
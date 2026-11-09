using NewBanking.Infrastructure;
using NewBanking.Presentation;
using NewBanking.Presentation.Banners;


InterBankSettings.Configure(args);
ConsoleUI.Banner("Welcome to ChocboyBank");

new BankApp().Run();
using System.Security.Cryptography;

namespace NewBanking.BLM
{
    public static class AccountNumberGenerator
    {
        private const int AccountNumberLength = 10;

        public static string CreateUnique(List<Account> accounts)
        {
            string accountNumber;

            do
            {
                accountNumber = RandomNumberGenerator
                    .GetInt32(1_000_000_000, 2_000_000_000)
                    .ToString();
            }
            while (accounts.Any(a => a.AccountNumber == accountNumber));

            return accountNumber;
        }
    }
}

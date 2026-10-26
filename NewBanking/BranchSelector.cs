using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking
{
    public class BranchSelector
    {
        private static readonly string[] ValidBranches = { "Lagos", "Abuja" };

        public static string Prompt()
        {
            string? branch = null;

            while (branch == null)
            {
                Console.Write("Enter your branch city (Lagos or Abuja): ");
                string branchInput = ConsoleHelper.ReadTrimmed();

                if (string.IsNullOrWhiteSpace(branchInput))
                {
                    Console.WriteLine("Branch cannot be empty. Please try again.\n");
                    continue;
                }

                branch = Array.Find(
                    ValidBranches,
                    b => b.Equals(branchInput, StringComparison.OrdinalIgnoreCase));

                if (branch == null)
                {
                    Console.WriteLine("Invalid branch. Please enter Lagos or Abuja.\n");
                }
            }

            return branch;
        }
    }
}

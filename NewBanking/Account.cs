using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking
{
    public class Account
    {
        public string Name { get; set; } = "";
        public string Pin { get; set; } = "";
        public decimal Balance { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsFrozen { get; set; }
        public List<string> Transactions { get; set; } = new();
    }
}

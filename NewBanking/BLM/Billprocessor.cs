using NewBanking.Infrastructure;
using System;
using System.Linq;

namespace NewBanking.BLM
{
    
    public static class BillProcessor
    {
        
        public static string? Check(BillPurchaseRequest r)
        {
            if (string.IsNullOrWhiteSpace(r.Reference))
            {
                return "Reference is required.";
            }

            bool isAirtime = string.Equals(r.Type, "airtime", StringComparison.OrdinalIgnoreCase);
            bool isData = string.Equals(r.Type, "data", StringComparison.OrdinalIgnoreCase);

            if (!isAirtime && !isData)
            {
                return "Type must be airtime or data.";
            }

            if (!DataPlanCatalog.IsValidNetwork(r.Network))
            {
                return "Unknown network.";
            }

            string phone = r.PhoneNumber ?? "";

            if (phone.Length != 11 || !phone.StartsWith("0") || !phone.All(char.IsDigit))
            {
                return "Invalid phone number.";
            }

            if (decimal.Round(r.Amount, 2) != r.Amount)
            {
                return "Amount cannot have more than 2 decimal places.";
            }

            if (isAirtime)
            {
                if (r.Amount <= 50 || r.Amount > 50000)
                {
                    return "Airtime amount must be above 50 and not more than 50,000 naira.";
                }
            }
            else
            {
                DataPlan? plan = DataPlanCatalog.Find(r.PlanCode);

                if (plan == null)
                {
                    return "Unknown data plan.";
                }

                if (plan.Price != r.Amount)
                {
                    return "Amount does not match the plan price.";
                }
            }

            return null;
        }

        
        public static (int Status, TransferResponse Response) Process(BillPurchaseRequest request, bool ownCustomer)
        {
           
            string? problem = Check(request);

            if (problem != null)
            {
                return (400, new TransferResponse(false, problem));
            }

            string description = string.Equals(request.Type, "data", StringComparison.OrdinalIgnoreCase)
                ? $"data ({DataPlanCatalog.Find(request.PlanCode)!.Name}) for {request.PhoneNumber} on {request.Network}"
                : $"airtime for {request.PhoneNumber} on {request.Network}";

            lock (BankStorage.SyncRoot)
            {
                BillLedger ledger = BankStorage.LoadBillLedger();

                
                if (ledger.Records.ContainsKey(request.Reference))
                {
                    return (200, new TransferResponse(true, "Already processed."));
                }

                
                if (ownCustomer)
                {
                    ledger.OwnSales += request.Amount;
                }
                else
                {
                    ledger.Receivable += request.Amount;
                }

                ledger.Records[request.Reference] = new BillRecord
                {
                    Reference = request.Reference,
                    Source = ownCustomer ? "Own customer" : "Other bank",
                    Description = description,
                    Amount = request.Amount
                };

                try
                {
                    BankStorage.SaveBillLedger(ledger);
                }
                catch (Exception)
                {
                    return (500, new TransferResponse(false, "Could not save the purchase."));
                }

                return (200, new TransferResponse(true, $"Delivered {description}."));
            }
        }
    }
}
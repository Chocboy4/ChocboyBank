using NewBanking.BLM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NewBanking.Infrastructure
{
    public static class InterBankServer
    {

        private static readonly HashSet<string> Processed = new();

        public static void Start(List<Account> accounts)
        {
            if (!InterBankSettings.Enabled)
            {
                return;
            }

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{InterBankSettings.Port}/");
            listener.Prefixes.Add($"http://127.0.0.1:{InterBankSettings.Port}/");

            try
            {
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"Could not listen on port {InterBankSettings.Port}: {ex.Message}");
                Console.WriteLine("This session can send other-bank transfers but not receive them.");
                return;
            }

            //Console.WriteLine($"Listening for other-bank transfers on port {InterBankSettings.Port}.");

            Task.Run(() => ListenLoop(listener, accounts));
        }

        private static async Task ListenLoop(HttpListener listener, List<Account> accounts)
        {
            while (listener.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (Exception ex) when (
                    ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    break;
                }

                _ = Task.Run(() => Handle(context, accounts));
            }
        }

        private static void Handle(HttpListenerContext context, List<Account> accounts)
        {
            try
            {
                var (status, response) = Route(context, accounts);
                Write(context, status, response);
            }
            catch (Exception)
            {
                try
                {
                    Write(context, 500, new TransferResponse(false, "Internal error."));
                }
                catch (Exception)
                {

                }
            }
        }

        private static (int Status, object Body) Route(
            HttpListenerContext context, List<Account> accounts)
        {
            HttpListenerRequest req = context.Request;
            string? path = req.Url?.AbsolutePath;

            bool isTransfer = req.HttpMethod == "POST" && path == "/api/transfers/incoming";
            bool isPurchase = req.HttpMethod == "POST" && path == "/api/bills/purchase";
            bool isPlans = req.HttpMethod == "GET" && path == "/api/bills/data-plans";

            if (!isTransfer && !isPurchase && !isPlans)
            {
                return (404, new TransferResponse(false, "Not found."));
            }

            if (req.Headers["X-Api-Key"] != InterBankSettings.ApiKey)
            {
                return (401, new TransferResponse(false, "Invalid API key."));
            }

            // List the data plans ChocboyBank sells
            if (isPlans)
            {
                return (200, DataPlanCatalog.Plans);
            }

            string body;

            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            // Buy airtime / data (request from the other bank)
            if (isPurchase)
            {
                return Purchase(body);
            }

            // ----- existing transfer logic below, unchanged -----

            TransferRequest? request;

            try
            {
                request = JsonSerializer.Deserialize<TransferRequest>(body, InterBankSettings.Json);
            }
            catch (JsonException)
            {
                return (400, new TransferResponse(false, "Request body is not valid JSON."));
            }

            if (request == null)
            {
                return (400, new TransferResponse(false, "Empty request."));
            }

            return Credit(request, accounts);
        }

        // CHANGED: all checking and recording now lives in BillProcessor,
        // which ChocboyBank's own customers use too.
        private static (int Status, TransferResponse Response) Purchase(string body)
        {
            BillPurchaseRequest? request;

            try
            {
                request = JsonSerializer.Deserialize<BillPurchaseRequest>(body, InterBankSettings.Json);
            }
            catch (JsonException)
            {
                return (400, new TransferResponse(false, "Request body is not valid JSON."));
            }

            if (request == null)
            {
                return (400, new TransferResponse(false, "Empty request."));
            }

            return BillProcessor.Process(request, ownCustomer: false);
        }

        private static (int Status, TransferResponse Response) Credit(
            TransferRequest request, List<Account> accounts)
        {
            if (string.IsNullOrWhiteSpace(request.TransferId) ||
                string.IsNullOrWhiteSpace(request.ToAccount) ||
                string.IsNullOrWhiteSpace(request.FromAccount))
            {
                return (400, new TransferResponse(false, "TransferId, FromAccount and ToAccount are required."));
            }

            if (request.Amount <= 50 || decimal.Round(request.Amount, 2) != request.Amount)
            {
                return (400, new TransferResponse(false, "Amount must be Higher than 50 naira."));
            }

            lock (BankStorage.SyncRoot)
            {
                if (Processed.Contains(request.TransferId))
                {
                    return (200, new TransferResponse(true, "Already processed."));
                }


                Account? recipient = accounts.Find(a =>
                    !a.IsAdmin &&
                    a.Name.Equals(request.ToAccount.Trim(), StringComparison.OrdinalIgnoreCase));

                if (recipient == null)
                {
                    return (404, new TransferResponse(false, "Recipient account not found."));
                }

                recipient.Balance += request.Amount;
                recipient.Transactions.Add(
                    $"Received ₦{request.Amount:N2} from {request.FromAccount} at {request.FromBank} (ref {ShortRef(request.TransferId)})");

                try
                {
                    BankStorage.SaveAccounts(accounts);
                }
                catch (Exception)
                {
                    recipient.Balance -= request.Amount;
                    recipient.Transactions.RemoveAt(recipient.Transactions.Count - 1);
                    return (500, new TransferResponse(false, "Could not save the transfer."));
                }

                Processed.Add(request.TransferId);
                return (200, new TransferResponse(true, "Transfer received."));
            }
        }

        private static string ShortRef(string id) => id.Length > 8 ? id[..8] : id;

        private static void Write(HttpListenerContext context, int status, object body)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(body, body.GetType(), InterBankSettings.Json);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
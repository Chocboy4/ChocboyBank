using NewBanking.BLM;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NewBanking.Infrastructure
{
    public static class InterBankClient
    {
        
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static async Task<SendResult> SendAsync(TransferRequest request)
        {
            try
            {
                using var message = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{InterBankSettings.PeerUrl}/api/transfers/incoming");

                message.Headers.Add("X-Api-Key", InterBankSettings.ApiKey);
                message.Content = JsonContent.Create(request, options: InterBankSettings.Json);

                using HttpResponseMessage response = await Http.SendAsync(message);

                string body = await response.Content.ReadAsStringAsync();
                TransferResponse? parsed = TryParse(body);
                int code = (int)response.StatusCode;

                if (response.IsSuccessStatusCode && parsed is { Success: true })
                {
                    return new SendResult(SendOutcome.Delivered, parsed.Message);
                }

                
                if (code is >= 400 and < 500)
                {
                    return new SendResult(
                        SendOutcome.Rejected,
                        parsed?.Message ?? $"The other bank rejected the request (HTTP {code}).");
                }

                
                return new SendResult(SendOutcome.Unknown, $"Unexpected response from the other bank (HTTP {code}).");
            }
            catch (HttpRequestException ex) when (
                ex.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused })
            {
                
                return new SendResult(SendOutcome.NotDelivered, "Could not reach the other bank. Is it running?");
            }
            catch (TaskCanceledException)
            {
                return new SendResult(SendOutcome.Unknown, "The other bank did not answer in time.");
            }
            catch (HttpRequestException ex)
            {
                return new SendResult(SendOutcome.Unknown, $"Connection problem: {ex.Message}");
            }
        }

        private static TransferResponse? TryParse(string body)
        {
            try
            {
                return JsonSerializer.Deserialize<TransferResponse>(body, InterBankSettings.Json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
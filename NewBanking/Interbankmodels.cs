using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking
{
    
    public record TransferRequest(
        string TransferId,
        string FromBank,
        string FromAccount,
        string ToAccount,
        decimal Amount);

    
    public record TransferResponse(bool Success, string Message);

    public enum SendOutcome
    {
        Delivered,   
        Rejected,    
        NotDelivered, 
        Unknown      
    }

    public record SendResult(SendOutcome Outcome, string Message);
}
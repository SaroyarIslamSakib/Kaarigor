using System;
using System.Collections.Generic;
using System.Text;

namespace CleanSolution.Domain.Utilities
{
    public interface IEmailUtility
    {
        Task SendEmailAsync(string reciverEmail, string receiverName, string subject, string body);

    }
}

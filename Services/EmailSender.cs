using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PT.Services
{
    public class EmailSender : IEmailSender
    {
        public string SendGridApiKey { get; }
        public string SendGridSendingUser { get; }

        public EmailSender()
        {
            SendGridApiKey = Program.Config.GetValue<string>("SendGridApiKey");
            SendGridSendingUser = "NOREPLY";
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(SendGridApiKey, subject, message, email);
        }

        public Task Execute(string apiKey, string subject, string message, string email)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("noreply@pro-trades.com", SendGridSendingUser),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            //Disable click tracking, see https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);

            return client.SendEmailAsync(msg);
        }
    }

    public static class EmailTest
    {
        public static void Send()
        {
            Execute().Wait();
        }

        private static async Task Execute()
        {
            var apiKey = Program.Config.GetValue<string>("SendGridApiKey");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("noreply@pro-trades.com", "NOREPLY");
            var subject = "Sending with SendGrid is Fun";
            var to = new EmailAddress("Kellan.Nealy@gmail.com", "Kellan Nealy");
            var plainTextContent = "and easy to do anywhere, even with C#";
            var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }
}

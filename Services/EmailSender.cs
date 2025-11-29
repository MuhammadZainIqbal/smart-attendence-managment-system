using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Services
{
    /// <summary>
    /// Implements IEmailSender using a configured SMTP client (Gmail).
    /// Credentials and server settings are loaded from IConfiguration.
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly string _email;
        private readonly string _appPassword;
        private readonly string _smtpServer;
        private readonly int _port;

        public EmailSender(IConfiguration config)
        {
            // Load email configuration from the "EmailSettings" section
            var emailSettings = config.GetSection("EmailSettings");
            _email = emailSettings["Email"] ?? throw new ArgumentNullException("EmailSettings:Email is missing.");
            _appPassword = emailSettings["AppPassword"] ?? throw new ArgumentNullException("EmailSettings:AppPassword is missing.");
            _smtpServer = emailSettings["SmtpServer"] ?? "smtp.gmail.com";
            _port = int.Parse(emailSettings["Port"] ?? "587");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_email, "Attendance Management System"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                // Add the recipient
                message.To.Add(new MailAddress(email));

                // Configure the SMTP client for Gmail
                using (var client = new SmtpClient(_smtpServer, _port))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_email, _appPassword);

                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                // Log the exception details to help with debugging
                Console.WriteLine($"Error sending email to {email}: {ex.Message}");
                // Re-throw or handle as appropriate for your application
                throw;
            }
        }
    }
}
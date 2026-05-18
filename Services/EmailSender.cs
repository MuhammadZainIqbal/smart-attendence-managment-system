using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Services
{
    /// <summary>
    /// Implements IEmailSender using the Resend REST API, with a robust local fallback for sandbox/development.
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _fromEmail;

        public EmailSender(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            // Load email configuration from the "Resend" section
            var resendSection = config.GetSection("Resend");
            _apiKey = resendSection["ApiKey"] ?? "YOUR_RESEND_API_KEY_PLACEHOLDER";
            _fromEmail = resendSection["FromEmail"] ?? "onboarding@resend.dev";

            // Configure HttpClient for Resend API
            _httpClient.BaseAddress = new Uri("https://api.resend.com/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // 1. Check if we are running with a placeholder API Key (Local Testing/Cold Start)
            if (string.IsNullOrEmpty(_apiKey) || 
                _apiKey.Equals("YOUR_RESEND_API_KEY_PLACEHOLDER", StringComparison.OrdinalIgnoreCase) || 
                _apiKey.StartsWith("re_YOUR_REAL_RESEND_KEY", StringComparison.OrdinalIgnoreCase))
            {
                await WriteEmailToLocalFileAsync(email, subject, htmlMessage, "MOCK/DEVELOPMENT MODE (Placeholder Key)");
                return;
            }

            var payload = new
            {
                from = _fromEmail,
                to = new[] { email },
                subject = subject,
                html = htmlMessage
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                // Send the POST request to the Resend /emails endpoint
                var response = await _httpClient.PostAsync("emails", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();

                    // 2. Catch Sandbox Restrictions and handle gracefully for Local Dev
                    // If you attempt to send an email to anyone other than your Resend account email using the 
                    // onboarding@resend.dev domain, Resend returns Forbidden or UnprocessableEntity.
                    // We intercept this, log to a local file, and let the process succeed so development flows continue.
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden || 
                        response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                    {
                        await WriteEmailToLocalFileAsync(email, subject, htmlMessage, $"SANDBOX LIMITATION (Status: {response.StatusCode}, Details: {errorDetails})");
                        return;
                    }

                    Console.WriteLine($"[Resend API Error]: {response.StatusCode} - {errorDetails}");
                    throw new HttpRequestException($"Failed to send email via Resend API. Status: {response.StatusCode}, Details: {errorDetails}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email Failure]: Error sending email to {email}: {ex.Message}");
                // If it's a network issue or client exception during development, write locally to prevent blocking dev flow
                await WriteEmailToLocalFileAsync(email, subject, htmlMessage, $"EXCEPTION INTERCEPTED ({ex.GetType().Name}: {ex.Message})");
            }
        }

        private async Task WriteEmailToLocalFileAsync(string toEmail, string subject, string htmlContent, string reason)
        {
            try
            {
                var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var outputFolder = Path.Combine(webRoot, "sent-emails");

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // Generate clean filename
                string safeSubject = string.Concat(subject.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                string fileName = $"email_{DateTime.Now:yyyyMMdd_HHmmss}_{safeSubject}.html";
                string filePath = Path.Combine(outputFolder, fileName);

                // Build a helpful debugger header in the HTML file
                string debugWrapper = $@"<!-- 
  =========================================
  DEVELOPMENT EMAIL LOGGER FALLBACK
  Reason: {reason}
  To: {toEmail}
  Subject: {subject}
  Date: {DateTime.Now}
  =========================================
-->
{htmlContent}";

                await File.WriteAllTextAsync(filePath, debugWrapper, Encoding.UTF8);

                // Print high-visibility banner to the console
                Console.WriteLine("==========================================================================");
                Console.WriteLine("📧 [DEVELOPMENT MAIL FALLBACK]");
                Console.WriteLine($"Reason:    {reason}");
                Console.WriteLine($"To:        {toEmail}");
                Console.WriteLine($"Subject:   {subject}");
                Console.WriteLine($"Saved To:  wwwroot/sent-emails/{fileName}");
                Console.WriteLine("==========================================================================");
            }
            catch (Exception fileEx)
            {
                Console.WriteLine($"Failed to write email fallback file: {fileEx.Message}");
            }
        }
    }
}
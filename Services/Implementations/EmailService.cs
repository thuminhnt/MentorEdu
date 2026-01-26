using MentorEdu.Models;
using MentorEdu.Services.Interfaces;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MentorEdu.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var client = new SendGridClient(_emailSettings.SendGridApiKey);
                var from = new EmailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);

                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("SendGrid error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                    throw new Exception($"Failed to send email: {response.StatusCode}");
                }

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendVerificationEmailAsync(string toEmail, string fullName, string verificationToken)
        {
            var verificationUrl = $"{_emailSettings.FrontendBaseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(toEmail)}";

            var subject = "Verify your EduMentor account";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ font-family: 'Google Sans', Roboto, -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif; background-color: #ffffff; margin: 0; padding: 0; }}
        .email-wrapper {{ width: 100%; padding: 40px 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #dadce0; border-radius: 8px; overflow: hidden; }}
        .logo-section {{ display: flex; align-items: center; justify-content: center; gap: 14px; padding: 48px 40px 32px; }}
        .logo-section img {{ height: 40px; width: 40px; object-fit: contain; }}
        .logo-text {{ font-size: 28px; font-weight: 500; color: #1a73e8; letter-spacing: -0.5px; }}
        .content {{ padding: 0 40px 40px; }}
        .greeting {{ color: #202124; font-size: 24px; font-weight: 400; margin: 0 0 24px 0; }}
        .text {{ color: #5f6368; font-size: 14px; line-height: 1.6; margin: 0 0 32px 0; }}
        .button-container {{ text-align: center; margin: 0 0 32px 0; }}
        .button {{ display: inline-block; background: #ffffff; color: #1a73e8 !important; padding: 12px 24px; text-decoration: none; border: 2px solid #1a73e8; border-radius: 4px; font-weight: 500; font-size: 14px; transition: background-color 0.2s; }}
        .button:hover {{ background-color: #f1f5f9; }}
        .note {{ background: #fef7e0; padding: 16px; border-radius: 4px; border-left: 4px solid #f9ab00; margin: 0 0 32px 0; }}
        .note-text {{ color: #5f6368; font-size: 13px; margin: 0; line-height: 1.5; }}
        .link-section {{ padding-top: 24px; border-top: 1px solid #e8eaed; }}
        .link-label {{ color: #5f6368; font-size: 12px; margin: 0 0 8px 0; }}
        .link {{ color: #1a73e8; font-size: 12px; word-break: break-all; text-decoration: none; }}
        .footer {{ padding: 24px 40px; text-align: center; border-top: 1px solid #e8eaed; }}
        .footer-text {{ color: #80868b; font-size: 12px; margin: 0; }}
    </style>
</head>
<body>
    <div class=""email-wrapper"">
        <div class=""container"">
            <div class=""logo-section"">
                <img src=""https://i.imgur.com/6QZ6g7U.png"" alt=""EduMentor Logo"">
                <span class=""logo-text"">EduMentor</span>
            </div>
            <div class=""content"">
                <p class=""greeting"">Hi {fullName},</p>
                <p class=""text"">Thank you for signing up for EduMentor. Please verify your email address to complete your registration and get started.</p>
                
                <div class=""button-container"">
                    <a href=""{verificationUrl}"" class=""button"">Verify Email Address</a>
                </div>
                
                <div class=""note"">
                    <p class=""note-text"">This link will expire in 24 hours. If you didn't create an account, you can safely ignore this email.</p>
                </div>
                
                <div class=""link-section"">
                    <p class=""link-label"">If the button doesn't work, copy and paste this link:</p>
                    <p><a href=""{verificationUrl}"" class=""link"">{verificationUrl}</a></p>
                </div>
            </div>
            <div class=""footer"">
                <p class=""footer-text"">© 2026 EduMentor. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetToken)
        {
            var resetUrl = $"{_emailSettings.FrontendBaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

            var subject = "Reset your EduMentor password";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ font-family: 'Google Sans', Roboto, -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif; background-color: #ffffff; margin: 0; padding: 0; }}
        .email-wrapper {{ width: 100%; padding: 40px 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #dadce0; border-radius: 8px; overflow: hidden; }}
        .logo-section {{ display: flex; align-items: center; justify-content: center; gap: 14px; padding: 48px 40px 32px; }}
        .logo-section img {{ height: 40px; width: 40px; object-fit: contain; }}
        .logo-text {{ font-size: 28px; font-weight: 500; color: #1a73e8; letter-spacing: -0.5px; }}
        .content {{ padding: 0 40px 40px; }}
        .greeting {{ color: #202124; font-size: 24px; font-weight: 400; margin: 0 0 24px 0; }}
        .text {{ color: #5f6368; font-size: 14px; line-height: 1.6; margin: 0 0 32px 0; }}
        .button-container {{ text-align: center; margin: 0 0 32px 0; }}
        .button {{ display: inline-block; background: #ffffff; color: #1a73e8 !important; padding: 12px 24px; text-decoration: none; border: 2px solid #1a73e8; border-radius: 4px; font-weight: 500; font-size: 14px; transition: background-color 0.2s; }}
        .button:hover {{ background-color: #f1f5f9; }}
        .note {{ background: #fef7e0; padding: 16px; border-radius: 4px; border-left: 4px solid #f9ab00; margin: 0 0 32px 0; }}
        .note-text {{ color: #5f6368; font-size: 13px; margin: 0; line-height: 1.5; }}
        .link-section {{ padding-top: 24px; border-top: 1px solid #e8eaed; }}
        .link-label {{ color: #5f6368; font-size: 12px; margin: 0 0 8px 0; }}
        .link {{ color: #1a73e8; font-size: 12px; word-break: break-all; text-decoration: none; }}
        .footer {{ padding: 24px 40px; text-align: center; border-top: 1px solid #e8eaed; }}
        .footer-text {{ color: #80868b; font-size: 12px; margin: 0; }}
    </style>
</head>
<body>
    <div class=""email-wrapper"">
        <div class=""container"">
            <div class=""logo-section"">
                <img src=""https://i.imgur.com/6QZ6g7U.png"" alt=""EduMentor Logo"">
                <span class=""logo-text"">EduMentor</span>
            </div>
            <div class=""content"">
                <p class=""greeting"">Hi {fullName},</p>
                <p class=""text"">We received a request to reset your EduMentor account password. Click the button below to create a new password.</p>
                
                <div class=""button-container"">
                    <a href=""{resetUrl}"" class=""button"">Reset Password</a>
                </div>
                
                <div class=""note"">
                    <p class=""note-text"">This link will expire in 1 hour. If you didn't request a password reset, you can safely ignore this email.</p>
                </div>
                
                <div class=""link-section"">
                    <p class=""link-label"">If the button doesn't work, copy and paste this link:</p>
                    <p><a href=""{resetUrl}"" class=""link"">{resetUrl}</a></p>
                </div>
            </div>
            <div class=""footer"">
                <p class=""footer-text"">© 2026 EduMentor. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}

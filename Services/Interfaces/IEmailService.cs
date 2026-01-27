namespace MentorEdu.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendVerificationEmailAsync(string toEmail, string fullName, string verificationToken);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetToken);
        Task SendMentorApprovalNotificationAsync(string toEmail, string fullName);
        Task SendMentorRejectionNotificationAsync(string toEmail, string fullName, string reason);
    }
}

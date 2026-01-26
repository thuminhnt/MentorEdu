namespace MentorEdu.Models
{
    public class EmailSettings
    {
        public string SendGridApiKey { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    }
}

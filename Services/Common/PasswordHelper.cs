namespace MentorEdu.Services.Common
{
    /// <summary>
    /// Static helper cho các thao tác liên quan đến password (BCrypt).
    /// Dùng để tránh duplicate logic giữa AuthService và AdminService.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>Hash plain-text password bằng BCrypt.</summary>
        public static string Hash(string password)
            => BCrypt.Net.BCrypt.HashPassword(password);

        /// <summary>Kiểm tra plain-text password có khớp với hash không.</summary>
        public static bool Verify(string password, string passwordHash)
            => BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}

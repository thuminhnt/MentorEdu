using System.ComponentModel.DataAnnotations;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Admin
{
    #region User Management

    /// <summary>Admin Create User DTO</summary>
    public class AdminCreateUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public UserRole Role { get; set; }

        [Phone(ErrorMessage = "Invalid phone format")]
        public string? Phone { get; set; }

        public string? Gender { get; set; }
        public string? School { get; set; }
        public string? Major { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        public bool IsVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Admin Update User DTO</summary>
    public class AdminUpdateUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [MaxLength(100, ErrorMessage = "Password must be less than 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string? Password { get; set; }

        [Phone(ErrorMessage = "Invalid phone format")]
        public string? Phone { get; set; }

        public string? Gender { get; set; }
        public string? School { get; set; }
        public string? Major { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}

using MentorEdu.DTOs.User;
using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Extensions
{
    /// <summary>
    /// Extension methods để map User entity sang UserDto — tránh lặp lại manual mapping.
    /// </summary>
    public static class UserMappingExtensions
    {
        /// <summary>
        /// Map User (bao gồm UserProfile và MentorProfile) sang UserDetailDto.
        /// </summary>
        public static UserDetailDto ToDetailDto(this User user)
        {
            var dto = new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                IsVerified = user.IsVerified,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                FullName = user.UserProfile?.FullName,
                AvatarUrl = user.UserProfile?.AvatarUrl,
                Phone = user.UserProfile?.Phone,
                Gender = user.UserProfile?.Gender,
                School = user.UserProfile?.School,
                Major = user.UserProfile?.Major,
                Bio = user.UserProfile?.Bio,
                City = user.UserProfile?.City,
                Country = user.UserProfile?.Country
            };

            if (user.Role == UserRole.Mentor && user.MentorProfile != null)
            {
                dto.MentorProfile = new MentorProfileDto
                {
                    Title = user.MentorProfile.Title,
                    HourlyRate = user.MentorProfile.HourlyRate,
                    PackagePrice = user.MentorProfile.PackagePrice,
                    ExperienceYears = user.MentorProfile.ExperienceYears,
                    Introduction = user.MentorProfile.Introduction,
                    AvailabilityNote = user.MentorProfile.AvailabilityNote,
                    RatingAvg = user.MentorProfile.RatingAvg,
                    RatingCount = user.MentorProfile.RatingCount
                };
            }

            return dto;
        }

        /// <summary>
        /// Map User sang UserBasicDto.
        /// </summary>
        public static UserBasicDto ToBasicDto(this User user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.UserProfile?.FullName,
            AvatarUrl = user.UserProfile?.AvatarUrl,
            Role = user.Role,
            IsVerified = user.IsVerified,
            IsActive = user.IsActive
        };
    }
}

using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IAdminStatsService
    {
        Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync();
    }
}

using MentorEdu.Repositories.Interfaces;

namespace MentorEdu.Services.Common
{
    /// <summary>
    /// Base Service v?i UnitOfWork
    /// </summary>
    public abstract class BaseService
    {
        protected readonly IUnitOfWork _unitOfWork;

        protected BaseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
    }
}

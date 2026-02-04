using MentorEdu.DTOs.Chat;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Request;
using MentorEdu.DTOs.User;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class RequestService : BaseService, IRequestService
    {
        private readonly IChatService _chatService;
        private readonly INotificationService _notificationService;

        public RequestService(IUnitOfWork unitOfWork, IChatService chatService, INotificationService notificationService) : base(unitOfWork)
        {
            _chatService = chatService;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<RequestDetailDto>> GetByIdAsync(long requestId)
        {
            var request = await _unitOfWork.Requests.GetByIdWithDetailsAsync(requestId);
            
            if (request == null)
                return ServiceResult<RequestDetailDto>.NotFoundResult("Request not found");

            var dto = MapToDetailDto(request);
            return ServiceResult<RequestDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<PagedResponse<RequestListDto>>> GetRequestsAsync(SearchRequestDto searchDto)
        {
            var (items, totalCount) = await _unitOfWork.Requests.GetPagedWithFiltersAsync(
                searchDto.PageNumber,
                searchDto.PageSize,
                searchDto.CategoryId,
                searchDto.Status,
                searchDto.Keyword);

            var dtos = items.Select(MapToListDto);
            var response = PagedResponse<RequestListDto>.Create(dtos, searchDto.PageNumber, searchDto.PageSize, totalCount);
            
            return ServiceResult<PagedResponse<RequestListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<RequestDetailDto>> CreateRequestAsync(long studentId, CreateRequestDto request)
        {
            // Validate student
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null || student.Role != UserRole.Student)
                return ServiceResult<RequestDetailDto>.FailResult("Only students can create requests");

            // Validate category
            if (request.CategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId.Value);
                if (category == null)
                    return ServiceResult<RequestDetailDto>.FailResult("Category does not exist");
            }

            // Validate mentor if specified
            if (request.MentorId.HasValue)
            {
                var mentor = await _unitOfWork.Users.GetByIdWithProfilesAsync(request.MentorId.Value);
                if (mentor == null || mentor.Role != UserRole.Mentor)
                    return ServiceResult<RequestDetailDto>.FailResult("Invalid mentor specified");

                // Check if already has pending request for this mentor
                var existingRequest = await _unitOfWork.Requests.GetPendingRequestByStudentAndMentorAsync(studentId, request.MentorId.Value);
                if (existingRequest != null)
                    return ServiceResult<RequestDetailDto>.FailResult("You already have a pending request with this mentor. Please wait for their response.");
            }

            var newRequest = new Request
            {
                StudentId = studentId,
                MentorId = request.MentorId,
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId,
                ExpectedBudget = request.ExpectedBudget,
                Status = RequestStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // If budget specified, deduct from wallet and create escrow
            var budget = request.ExpectedBudget ?? 0;
            if (budget > 0)
            {
                var studentWallet = await _unitOfWork.Wallets.GetByUserIdAsync(studentId);
                if (studentWallet == null)
                {
                    studentWallet = new Wallet
                    {
                        UserId = studentId,
                        Balance = 0,
                        LockedBalance = 0,
                        Currency = "VND",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Wallets.AddAsync(studentWallet);
                    await _unitOfWork.SaveChangesAsync();
                }

                var availableBalance = studentWallet.Balance - studentWallet.LockedBalance;
                if (availableBalance < budget)
                {
                    return ServiceResult<RequestDetailDto>.FailResult($"Insufficient balance. Required: {budget:N0} VND, Available: {availableBalance:N0} VND. Please top up your wallet.");
                }

                // Move funds to locked balance (escrow)
                studentWallet.Balance -= budget;
                studentWallet.LockedBalance += budget;
                studentWallet.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Wallets.Update(studentWallet);
            }

            await _unitOfWork.Requests.AddAsync(newRequest);
            await _unitOfWork.SaveChangesAsync();

            // Create escrow if budget > 0
            if (budget > 0)
            {
                var escrow = new Escrow
                {
                    OrderId = null, // Will be linked when proposal is accepted and order is created
                    Amount = budget,
                    Status = EscrowStatus.Holding,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Escrows.AddAsync(escrow);

                // Create transaction record
                var studentWallet = await _unitOfWork.Wallets.GetByUserIdAsync(studentId);
                var transaction = new Transaction
                {
                    WalletId = studentWallet!.Id,
                    OrderId = null,
                    Type = TransactionType.EscrowHold,
                    Amount = budget,
                    Status = TransactionStatus.Success,
                    Description = $"Escrow hold for request: {request.Title}",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
            }

            return await GetByIdAsync(newRequest.Id);
        }

        public async Task<ServiceResult<RequestDetailDto>> UpdateRequestAsync(long requestId, long userId, UpdateRequestDto request)
        {
            var existingRequest = await _unitOfWork.Requests.GetByIdAsync(requestId);
            
            if (existingRequest == null)
                return ServiceResult<RequestDetailDto>.NotFoundResult("Request not found");

            if (existingRequest.StudentId != userId)
                return ServiceResult<RequestDetailDto>.ForbiddenResult("You do not have permission to edit this request");

            if (existingRequest.Status != RequestStatus.Open)
                return ServiceResult<RequestDetailDto>.FailResult("Only open requests can be edited");

            // Validate category
            if (request.CategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId.Value);
                if (category == null)
                    return ServiceResult<RequestDetailDto>.FailResult("Category does not exist");
            }

            existingRequest.Title = request.Title;
            existingRequest.Description = request.Description;
            existingRequest.CategoryId = request.CategoryId;
            existingRequest.ExpectedBudget = request.ExpectedBudget;
            existingRequest.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Requests.Update(existingRequest);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(requestId);
        }

        public async Task<ServiceResult> DeleteRequestAsync(long requestId, long userId)
        {
            var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
            
            if (request == null)
                return ServiceResult.NotFoundResult("Request not found");

            if (request.StudentId != userId)
                return ServiceResult.ForbiddenResult("You do not have permission to delete this request");

            if (request.Status == RequestStatus.InOrder)
                return ServiceResult.FailResult("Cannot delete request with active order");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                // Check if there's money in escrow that needs to be refunded
                var budget = request.ExpectedBudget ?? 0;
                if (budget > 0)
                {
                    // Find pending escrow for this request
                    var escrow = await _unitOfWork.Escrows.GetPendingEscrowByAmountAsync(budget);
                    
                    if (escrow != null && escrow.Status == EscrowStatus.Holding)
                    {
                        // Refund to student wallet
                        var studentWallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
                        if (studentWallet != null)
                        {
                            studentWallet.Balance += budget;
                            studentWallet.LockedBalance -= budget;
                            studentWallet.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.Wallets.Update(studentWallet);

                            // Update escrow status
                            escrow.Status = EscrowStatus.Refunded;
                            escrow.RefundedAt = DateTime.UtcNow;
                            _unitOfWork.Escrows.Update(escrow);

                            // Create refund transaction
                            var transaction = new Transaction
                            {
                                WalletId = studentWallet.Id,
                                OrderId = null,
                                Type = TransactionType.EscrowRefund,
                                Amount = budget,
                                Status = TransactionStatus.Success,
                                Description = $"Refund from cancelled request: {request.Title}",
                                CreatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork.Transactions.AddAsync(transaction);
                        }
                    }
                }

                // Soft delete
                request.Status = RequestStatus.Deleted;
                request.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Requests.Update(request);

                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("Request deleted and funds refunded successfully");
            });
        }

        public async Task<ServiceResult<PagedResponse<RequestListDto>>> GetMyRequestsAsync(long studentId, int pageNumber, int pageSize)
        {
            var requests = await _unitOfWork.Requests.GetByStudentIdAsync(studentId);
            var totalCount = requests.Count();

            var pagedRequests = requests
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var dtos = pagedRequests.Select(MapToListDto);
            var response = PagedResponse<RequestListDto>.Create(dtos, pageNumber, pageSize, totalCount);

            return ServiceResult<PagedResponse<RequestListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> CloseRequestAsync(long requestId, long userId)
        {
            var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
            
            if (request == null)
                return ServiceResult.NotFoundResult("Request not found");

            if (request.StudentId != userId)
                return ServiceResult.ForbiddenResult("You do not have permission to close this request");

            if (request.Status != RequestStatus.Open && request.Status != RequestStatus.Matched)
                return ServiceResult.FailResult("Cannot close this request");

            request.Status = RequestStatus.Closed;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Requests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Request closed successfully");
        }

        public async Task<ServiceResult<PagedResponse<RequestListDto>>> GetOpenRequestsAsync(SearchRequestDto searchDto)
        {
            searchDto.Status = RequestStatus.Open;
            return await GetRequestsAsync(searchDto);
        }

        public async Task<ServiceResult<PagedResponse<RequestListDto>>> GetReceivedRequestsAsync(long mentorId, int pageNumber, int pageSize)
        {
            // Get requests targeted to this mentor
            var allRequests = await _unitOfWork.Requests.GetByMentorIdAsync(mentorId);
            var totalCount = allRequests.Count();

            var pagedRequests = allRequests
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var dtos = pagedRequests.Select(MapToListDto);
            var response = PagedResponse<RequestListDto>.Create(dtos, pageNumber, pageSize, totalCount);

            return ServiceResult<PagedResponse<RequestListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<AcceptRequestResultDto>> AcceptRequestAsync(long requestId, long mentorId)
        {
            var request = await _unitOfWork.Requests.GetByIdWithDetailsAsync(requestId);
            
            if (request == null)
                return ServiceResult<AcceptRequestResultDto>.NotFoundResult("Request not found");

            // Verify mentor is the target of this request
            if (request.MentorId != mentorId)
                return ServiceResult<AcceptRequestResultDto>.ForbiddenResult("You cannot accept this request");

            if (request.Status != RequestStatus.Open)
                return ServiceResult<AcceptRequestResultDto>.FailResult($"Request cannot be accepted. Current status: {request.Status}");

            // Update request status
            request.Status = RequestStatus.Accepted;
            request.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Requests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            // Notify student that their request was accepted
            await _notificationService.CreateNotificationAsync(
                request.StudentId,
                NotificationType.RequestAccepted,
                "Request Accepted",
                $"Your request '{request.Title}' has been accepted by the mentor."
            );

            // Create conversation between mentor and student
            var conversationResult = await _chatService.GetOrCreateConversationAsync(mentorId, new CreateConversationDto
            {
                ParticipantId = request.StudentId
            });

            var conversationId = conversationResult.Success && conversationResult.Data != null
                ? conversationResult.Data.Id
                : 0;

            return ServiceResult<AcceptRequestResultDto>.SuccessResult(new AcceptRequestResultDto
            {
                RequestId = requestId,
                ConversationId = conversationId,
                Message = "Request accepted successfully"
            });
        }

        public async Task<ServiceResult> RejectRequestAsync(long requestId, long mentorId, string? reason)
        {
            var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
            
            if (request == null)
                return ServiceResult.NotFoundResult("Request not found");

            // Verify mentor is the target of this request  
            if (request.MentorId != mentorId)
                return ServiceResult.ForbiddenResult("You cannot reject this request");

            if (request.Status != RequestStatus.Open)
                return ServiceResult.FailResult($"Request cannot be rejected. Current status: {request.Status}");

            // Update request status
            request.Status = RequestStatus.Rejected;
            request.RejectReason = reason;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Requests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            // Notify student that their request was rejected
            var reasonText = !string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "";
            await _notificationService.CreateNotificationAsync(
                request.StudentId,
                NotificationType.RequestRejected,
                "Request Rejected",
                $"Your request '{request.Title}' has been rejected.{reasonText}"
            );

            return ServiceResult.SuccessResult("Request rejected successfully");
        }

        #region Private Methods

        private RequestListDto MapToListDto(Request request)
        {
            return new RequestListDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description?.Length > 200 
                    ? request.Description.Substring(0, 200) + "..." 
                    : request.Description,
                CategoryName = request.Category?.Name,
                CategoryId = request.CategoryId,
                ExpectedBudget = request.ExpectedBudget,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                ProposalCount = request.Proposals?.Count ?? 0,
                StudentId = request.StudentId,
                StudentName = request.Student?.UserProfile?.FullName,
                StudentAvatar = request.Student?.UserProfile?.AvatarUrl
            };
        }

        private RequestDetailDto MapToDetailDto(Request request)
        {
            return new RequestDetailDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId,
                CategoryName = request.Category?.Name,
                ExpectedBudget = request.ExpectedBudget,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                Student = new UserBasicDto
                {
                    Id = request.Student.Id,
                    Email = request.Student.Email,
                    FullName = request.Student.UserProfile?.FullName,
                    AvatarUrl = request.Student.UserProfile?.AvatarUrl,
                    Role = request.Student.Role,
                    IsVerified = request.Student.IsVerified,
                    IsActive = request.Student.IsActive
                },
                Proposals = request.Proposals?.Select(p => new ProposalListDto
                {
                    Id = p.Id,
                    RequestId = p.RequestId ?? 0,
                    Price = p.Price,
                    Message = p.Message,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    MentorId = p.MentorId,
                    MentorName = p.Mentor?.UserProfile?.FullName,
                    MentorAvatar = p.Mentor?.UserProfile?.AvatarUrl,
                    MentorTitle = p.Mentor?.MentorProfile?.Title,
                    MentorRating = p.Mentor?.MentorProfile?.RatingAvg,
                    MentorRatingCount = p.Mentor?.MentorProfile?.RatingCount
                }).ToList(),
                ProposalCount = request.Proposals?.Count ?? 0
            };
        }

        #endregion
    }
}

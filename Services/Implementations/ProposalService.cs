using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Request;
using MentorEdu.DTOs.User;
using MentorEdu.DTOs.Chat;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class ProposalService : BaseService, IProposalService
    {
        private readonly IChatService _chatService;
        private readonly INotificationService _notificationService;

        public ProposalService(IUnitOfWork unitOfWork, IChatService chatService, INotificationService notificationService) : base(unitOfWork)
        {
            _chatService = chatService;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<ProposalDetailDto>> GetByIdAsync(long proposalId)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdWithDetailsAsync(proposalId);
            
            if (proposal == null)
                return ServiceResult<ProposalDetailDto>.NotFoundResult("Proposal not found");

            var dto = MapToDetailDto(proposal);
            return ServiceResult<ProposalDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<ProposalDetailDto>> CreateProposalAsync(long mentorId, CreateProposalDto request)
        {
            // Validate mentor
            var mentor = await _unitOfWork.Users.GetByIdAsync(mentorId);
            if (mentor == null || mentor.Role != UserRole.Mentor)
                return ServiceResult<ProposalDetailDto>.FailResult("Only mentors can submit proposals");

            // Validate request
            var requestEntity = await _unitOfWork.Requests.GetByIdAsync(request.RequestId);
            if (requestEntity == null)
                return ServiceResult<ProposalDetailDto>.NotFoundResult("Request not found");

            if (requestEntity.Status != RequestStatus.Open)
                return ServiceResult<ProposalDetailDto>.FailResult("Request is no longer open for proposals");

            // Check if mentor already has a pending proposal
            var existingProposal = await _unitOfWork.Proposals.GetByRequestAndMentorAsync(request.RequestId, mentorId);
            if (existingProposal != null && existingProposal.Status == ProposalStatus.Pending)
                return ServiceResult<ProposalDetailDto>.FailResult("You already have a pending proposal for this request");

            var proposal = new Proposal
            {
                RequestId = request.RequestId,
                MentorId = mentorId,
                Price = request.Price,
                Message = request.Message,
                Status = ProposalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Proposals.AddAsync(proposal);

            // Update request status to Matched if first proposal
            if (requestEntity.Status == RequestStatus.Open)
            {
                requestEntity.Status = RequestStatus.Matched;
                requestEntity.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Requests.Update(requestEntity);
            }

            await _unitOfWork.SaveChangesAsync();

            // TODO: Send notification to student

            return await GetByIdAsync(proposal.Id);
        }

        public async Task<ServiceResult<ProposalDetailDto>> UpdateProposalAsync(long proposalId, long mentorId, UpdateProposalDto request)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdAsync(proposalId);
            
            if (proposal == null)
                return ServiceResult<ProposalDetailDto>.NotFoundResult("Proposal not found");

            if (proposal.MentorId != mentorId)
                return ServiceResult<ProposalDetailDto>.ForbiddenResult("You do not have permission to edit this proposal");

            if (proposal.Status != ProposalStatus.Pending)
                return ServiceResult<ProposalDetailDto>.FailResult("Only pending proposals can be edited");

            proposal.Price = request.Price;
            proposal.Message = request.Message;
            proposal.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Proposals.Update(proposal);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(proposalId);
        }

        public async Task<ServiceResult> CancelProposalAsync(long proposalId, long mentorId)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdAsync(proposalId);
            
            if (proposal == null)
                return ServiceResult.NotFoundResult("Proposal not found");

            if (proposal.MentorId != mentorId)
                return ServiceResult.ForbiddenResult("You do not have permission to cancel this proposal");

            if (proposal.Status != ProposalStatus.Pending)
                return ServiceResult.FailResult("Only pending proposals can be cancelled");

            proposal.Status = ProposalStatus.Cancelled;
            proposal.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Proposals.Update(proposal);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Proposal cancelled successfully");
        }

        public async Task<ServiceResult<List<ProposalListDto>>> GetProposalsByRequestAsync(long requestId)
        {
            var proposals = await _unitOfWork.Proposals.GetByRequestIdAsync(requestId);

            var dtos = proposals.Select(MapToListDto).ToList();
            return ServiceResult<List<ProposalListDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<AcceptProposalResultDto>> AcceptProposalAsync(long proposalId, long studentId)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdWithDetailsAsync(proposalId);
            
            if (proposal == null)
                return ServiceResult<AcceptProposalResultDto>.NotFoundResult("Proposal not found");

            // Check ownership: proposal on request (direct) or on community post
            bool isOwner = proposal.RequestId.HasValue 
                ? proposal.Request?.StudentId == studentId 
                : proposal.Post?.AuthorId == studentId;

            if (!isOwner)
                return ServiceResult<AcceptProposalResultDto>.ForbiddenResult("You do not have permission to accept this proposal");

            if (proposal.Status != ProposalStatus.Pending)
                return ServiceResult<AcceptProposalResultDto>.FailResult("Proposal is no longer pending");

            // Get student wallet and validate balance
            var proposalPrice = proposal.Price ?? 0;
            var studentWallet = await _unitOfWork.Wallets.GetByUserIdAsync(studentId);
            
            if (studentWallet == null)
            {
                // Create wallet if not exists
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
            if (availableBalance < proposalPrice)
            {
                return ServiceResult<AcceptProposalResultDto>.FailResult($"Insufficient balance. Required: {proposalPrice:N0} VND, Available: {availableBalance:N0} VND. Please top up your wallet.");
            }

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                // Check if escrow already exists (from request creation) or need to create new one
                Escrow? existingEscrow = null;
                bool needToDeductWallet = true;

                if (proposal.RequestId.HasValue)
                {
                    // For direct request proposals, escrow should already exist from request creation
                    existingEscrow = await _unitOfWork.Escrows.GetPendingEscrowByAmountAsync(proposalPrice);

                    if (existingEscrow != null)
                    {
                        needToDeductWallet = false; // Funds already held from request creation
                    }
                }

                // Deduct funds from wallet only if escrow doesn't exist (community post case)
                if (needToDeductWallet)
                {
                    studentWallet.Balance -= proposalPrice;
                    studentWallet.LockedBalance += proposalPrice;
                    studentWallet.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Wallets.Update(studentWallet);
                }

                // Accept this proposal
                proposal.Status = ProposalStatus.Accepted;
                proposal.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Proposals.Update(proposal);

                // Reject other proposals for this request
                if (proposal.RequestId.HasValue)
                {
                    var otherProposals = await _unitOfWork.Proposals.GetByRequestIdAsync(proposal.RequestId.Value);
                    foreach (var other in otherProposals.Where(p => p.Id != proposalId && p.Status == ProposalStatus.Pending))
                    {
                        other.Status = ProposalStatus.Rejected;
                        other.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Proposals.Update(other);
                    }

                    // Update request status
                    var request = proposal.Request!;
                    request.Status = RequestStatus.InOrder;
                    request.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Requests.Update(request);
                }

                // Create order - now with InProgress status since payment is done
                var order = new Order
                {
                    RequestId = proposal.RequestId,
                    ProposalId = proposal.Id,
                    StudentId = studentId,
                    MentorId = proposal.MentorId,
                    BillingType = BillingType.Fixed,
                    AgreedPrice = proposal.Price,
                    Status = OrderStatus.InProgress,
                    StartedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Link existing escrow to order or create new one
                if (existingEscrow != null)
                {
                    // Link existing escrow to the new order
                    existingEscrow.OrderId = order.Id;
                    _unitOfWork.Escrows.Update(existingEscrow);
                }
                else
                {
                    // Create new escrow (for community post proposals)
                    var escrow = new Escrow
                    {
                        OrderId = order.Id,
                        Amount = proposalPrice,
                        Status = EscrowStatus.Holding,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Escrows.AddAsync(escrow);
                }

                // Create payment transaction record
                var transaction = new Transaction
                {
                    WalletId = studentWallet.Id,
                    OrderId = order.Id,
                    Type = TransactionType.EscrowHold,
                    Amount = proposalPrice,
                    Status = TransactionStatus.Success,
                    Description = $"Payment for order #{order.Id} - {proposal.Request?.Title ?? "Mentoring session"}",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                // Notify mentor that their proposal was accepted
                var proposalTitle = proposal.Request?.Title ?? proposal.Post?.Title ?? "your proposal";
                await _notificationService.CreateNotificationAsync(
                    proposal.MentorId,
                    NotificationType.ProposalAccepted,
                    "Proposal Accepted",
                    $"Your proposal for '{proposalTitle}' has been accepted! Order is now active."
                );

                // Create conversation between student and mentor
                var conversationResult = await _chatService.GetOrCreateConversationAsync(studentId, new CreateConversationDto
                {
                    ParticipantId = proposal.MentorId
                });

                var conversationId = conversationResult.Success && conversationResult.Data != null
                    ? conversationResult.Data.Id
                    : 0;

                return ServiceResult<AcceptProposalResultDto>.SuccessResult(new AcceptProposalResultDto
                {
                    ProposalId = proposalId,
                    ConversationId = conversationId,
                    Message = "Proposal accepted and payment processed successfully. Order is now active."
                });
            });
        }

        public async Task<ServiceResult> RejectProposalAsync(long proposalId, long studentId, string? reason = null)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdWithDetailsAsync(proposalId);
            
            if (proposal == null)
                return ServiceResult.NotFoundResult("Proposal not found");

            // Check ownership: either student from request or author of community post
            bool hasPermission = false;
            
            if (proposal.RequestId != null && proposal.Request != null)
            {
                // Direct request proposal
                hasPermission = proposal.Request.StudentId == studentId;
            }
            else if (proposal.PostId != null && proposal.Post != null)
            {
                // Community post proposal
                hasPermission = proposal.Post.AuthorId == studentId;
            }

            if (!hasPermission)
                return ServiceResult.ForbiddenResult("You do not have permission to reject this proposal");

            if (proposal.Status != ProposalStatus.Pending)
                return ServiceResult.FailResult("Proposal is no longer pending");

            proposal.Status = ProposalStatus.Rejected;
            proposal.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Proposals.Update(proposal);
            await _unitOfWork.SaveChangesAsync();

            // Notify mentor that their proposal was rejected
            var proposalTitle = proposal.Request?.Title ?? proposal.Post?.Title ?? "your proposal";
            var reasonText = !string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "";
            await _notificationService.CreateNotificationAsync(
                proposal.MentorId,
                NotificationType.ProposalRejected,
                "Proposal Rejected",
                $"Your proposal for '{proposalTitle}' has been rejected.{reasonText}"
            );

            return ServiceResult.SuccessResult("Proposal rejected successfully");
        }

        public async Task<ServiceResult<PagedResponse<ProposalListDto>>> GetMyProposalsAsync(long mentorId, int pageNumber, int pageSize)
        {
            var proposals = await _unitOfWork.Proposals.GetByMentorIdAsync(mentorId);
            var totalCount = proposals.Count();

            var pagedProposals = proposals
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var dtos = pagedProposals.Select(MapToListDto);
            var response = PagedResponse<ProposalListDto>.Create(dtos, pageNumber, pageSize, totalCount);

            return ServiceResult<PagedResponse<ProposalListDto>>.SuccessResult(response);
        }

        #region Private Methods

        private ProposalListDto MapToListDto(Proposal proposal)
        {
            return new ProposalListDto
            {
                Id = proposal.Id,
                RequestId = proposal.RequestId ?? 0,
                Price = proposal.Price,
                Message = proposal.Message,
                Status = proposal.Status,
                CreatedAt = proposal.CreatedAt,
                MentorId = proposal.MentorId,
                MentorName = proposal.Mentor?.UserProfile?.FullName,
                MentorAvatar = proposal.Mentor?.UserProfile?.AvatarUrl,
                MentorTitle = proposal.Mentor?.MentorProfile?.Title,
                MentorRating = proposal.Mentor?.MentorProfile?.RatingAvg,
                MentorRatingCount = proposal.Mentor?.MentorProfile?.RatingCount
            };
        }

        private ProposalDetailDto MapToDetailDto(Proposal proposal)
        {
            return new ProposalDetailDto
            {
                Id = proposal.Id,
                RequestId = proposal.RequestId ?? 0,
                RequestTitle = proposal.Request?.Title,
                Price = proposal.Price,
                Message = proposal.Message,
                Status = proposal.Status,
                CreatedAt = proposal.CreatedAt,
                UpdatedAt = proposal.UpdatedAt,
                Mentor = new MentorListDto
                {
                    Id = proposal.MentorId,
                    FullName = proposal.Mentor?.UserProfile?.FullName,
                    AvatarUrl = proposal.Mentor?.UserProfile?.AvatarUrl,
                    Title = proposal.Mentor?.MentorProfile?.Title,
                    HourlyRate = proposal.Mentor?.MentorProfile?.HourlyRate,
                    ExperienceYears = proposal.Mentor?.MentorProfile?.ExperienceYears,
                    RatingAvg = proposal.Mentor?.MentorProfile?.RatingAvg,
                    RatingCount = proposal.Mentor?.MentorProfile?.RatingCount
                },
                Student = proposal.Request?.Student != null ? new UserBasicDto
                {
                    Id = proposal.Request.Student.Id,
                    Email = proposal.Request.Student.Email,
                    FullName = proposal.Request.Student.UserProfile?.FullName,
                    AvatarUrl = proposal.Request.Student.UserProfile?.AvatarUrl,
                    Role = proposal.Request.Student.Role
                } : null
            };
        }

        #endregion
    }
}

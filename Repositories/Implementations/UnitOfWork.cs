using MentorEdu.Data;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace MentorEdu.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repositories
        private IUserRepository? _users;
        private IUserProfileRepository? _userProfiles;
        private IMentorProfileRepository? _mentorProfiles;
        private ICategoryRepository? _categories;
        private IHashtagRepository? _hashtags;
        private ICategoryHashtagRepository? _categoryHashtags;
        private IMentorCategoryRepository? _mentorCategories;
        private IMentorHashtagRepository? _mentorHashtags;
        private IRequestRepository? _requests;
        private IProposalRepository? _proposals;
        private IOrderRepository? _orders;
        private IOrderDetailRepository? _orderDetails;
        private IWorkSessionRepository? _workSessions;
        private ISessionTrackingRepository? _sessionTrackings;
        private IConversationRepository? _conversations;
        private IMessageRepository? _messages;
        private IWalletRepository? _wallets;
        private ITransactionRepository? _transactions;
        private IEscrowRepository? _escrows;
        private IReviewRepository? _reviews;
        private IReportRepository? _reports;
        private ICommunityPostRepository? _communityPosts;
        private ICommunityCommentRepository? _communityComments;
        private ICommunityPostHashtagRepository? _communityPostHashtags;
        private ICommunityPostFileRepository? _communityPostFiles;
        private IUserFollowRepository? _userFollows;
        private INotificationRepository? _notifications;
        private IMentorApplicationHistoryRepository? _mentorApplicationHistories;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // User & Profiles
        public IUserRepository Users => 
            _users ??= new UserRepository(_context);

        public IUserProfileRepository UserProfiles => 
            _userProfiles ??= new UserProfileRepository(_context);

        public IMentorProfileRepository MentorProfiles => 
            _mentorProfiles ??= new MentorProfileRepository(_context);

        // Category & Hashtag
        public ICategoryRepository Categories => 
            _categories ??= new CategoryRepository(_context);

        public IHashtagRepository Hashtags => 
            _hashtags ??= new HashtagRepository(_context);

        public ICategoryHashtagRepository CategoryHashtags => 
            _categoryHashtags ??= new CategoryHashtagRepository(_context);

        public IMentorCategoryRepository MentorCategories => 
            _mentorCategories ??= new MentorCategoryRepository(_context);

        public IMentorHashtagRepository MentorHashtags => 
            _mentorHashtags ??= new MentorHashtagRepository(_context);

        // Request, Proposal, Order
        public IRequestRepository Requests => 
            _requests ??= new RequestRepository(_context);

        public IProposalRepository Proposals => 
            _proposals ??= new ProposalRepository(_context);

        public IOrderRepository Orders => 
            _orders ??= new OrderRepository(_context);

        public IOrderDetailRepository OrderDetails => 
            _orderDetails ??= new OrderDetailRepository(_context);

        // Work Sessions
        public IWorkSessionRepository WorkSessions => 
            _workSessions ??= new WorkSessionRepository(_context);

        public ISessionTrackingRepository SessionTrackings => 
            _sessionTrackings ??= new SessionTrackingRepository(_context);

        // Chat
        public IConversationRepository Conversations => 
            _conversations ??= new ConversationRepository(_context);

        public IMessageRepository Messages => 
            _messages ??= new MessageRepository(_context);

        // Wallet & Payment
        public IWalletRepository Wallets => 
            _wallets ??= new WalletRepository(_context);

        public ITransactionRepository Transactions => 
            _transactions ??= new TransactionRepository(_context);

        public IEscrowRepository Escrows => 
            _escrows ??= new EscrowRepository(_context);

        // Review & Report
        public IReviewRepository Reviews => 
            _reviews ??= new ReviewRepository(_context);

        public IReportRepository Reports => 
            _reports ??= new ReportRepository(_context);

        // Community
        public ICommunityPostRepository CommunityPosts => 
            _communityPosts ??= new CommunityPostRepository(_context);

        public ICommunityCommentRepository CommunityComments => 
            _communityComments ??= new CommunityCommentRepository(_context);

        public ICommunityPostHashtagRepository CommunityPostHashtags => 
            _communityPostHashtags ??= new CommunityPostHashtagRepository(_context);

        public ICommunityPostFileRepository CommunityPostFiles => 
            _communityPostFiles ??= new CommunityPostFileRepository(_context);

        // Follow
        public IUserFollowRepository UserFollows => 
            _userFollows ??= new UserFollowRepository(_context);

        // Notification
        public INotificationRepository Notifications => 
            _notifications ??= new NotificationRepository(_context);

        // Mentor Application History
        public IMentorApplicationHistoryRepository MentorApplicationHistories =>
            _mentorApplicationHistories ??= new MentorApplicationHistoryRepository(_context);

        #region Transaction Management

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Execute operation with execution strategy for retry-on-failure support
        /// This wraps the entire transaction in the execution strategy
        /// </summary>
        public async Task ExecuteWithStrategyAsync(Func<Task> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(
                state: operation,
                operation: async (context, state, ct) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(ct);
                    await state();
                    await transaction.CommitAsync(ct);
                    return true;
                },
                verifySucceeded: null,
                cancellationToken: CancellationToken.None);
        }

        /// <summary>
        /// Execute operation with execution strategy and return result
        /// </summary>
        public async Task<T> ExecuteWithStrategyAsync<T>(Func<Task<T>> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(
                state: operation,
                operation: async (context, state, ct) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(ct);
                    var result = await state();
                    await transaction.CommitAsync(ct);
                    return result;
                },
                verifySucceeded: null,
                cancellationToken: CancellationToken.None);
        }

        #endregion

        #region Dispose

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

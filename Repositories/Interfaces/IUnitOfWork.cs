namespace MentorEdu.Repositories.Interfaces
{
    /// <summary>
    /// Unit of Work Interface - Qu?n l� transactions v� c�c repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // User & Profiles
        IUserRepository Users { get; }
        IUserProfileRepository UserProfiles { get; }
        IMentorProfileRepository MentorProfiles { get; }

        // Category & Hashtag
        ICategoryRepository Categories { get; }
        IHashtagRepository Hashtags { get; }
        ICategoryHashtagRepository CategoryHashtags { get; }

        // Request, Proposal, Order
        IRequestRepository Requests { get; }
        IProposalRepository Proposals { get; }
        IOrderRepository Orders { get; }
        IOrderDetailRepository OrderDetails { get; }

        // Work Sessions
        IWorkSessionRepository WorkSessions { get; }
        ISessionTrackingRepository SessionTrackings { get; }

        // Chat
        IConversationRepository Conversations { get; }
        IMessageRepository Messages { get; }

        // Wallet & Payment
        IWalletRepository Wallets { get; }
        ITransactionRepository Transactions { get; }
        IEscrowRepository Escrows { get; }

        // Review & Report
        IReviewRepository Reviews { get; }
        IReportRepository Reports { get; }

        // Community
        ICommunityPostRepository CommunityPosts { get; }
        ICommunityCommentRepository CommunityComments { get; }

        // Notification
        INotificationRepository Notifications { get; }

        // Transaction Management
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MentorEdu.Models;

namespace MentorEdu.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Users & Profiles
        public DbSet<User> AppUsers { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<MentorProfile> MentorProfiles { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Category & Hashtag
        public DbSet<Category> Categories { get; set; }
        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<MentorCategory> MentorCategories { get; set; }
        public DbSet<MentorHashtag> MentorHashtags { get; set; }
        public DbSet<CategoryHashtag> CategoryHashtags { get; set; }

        // Request, Proposal, Order
        public DbSet<Request> Requests { get; set; }
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // Work Sessions
        public DbSet<WorkSession> WorkSessions { get; set; }
        public DbSet<SessionTracking> SessionTrackings { get; set; }

        // Chat
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageFile> MessageFiles { get; set; }

        // Wallet & Payment
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Escrow> Escrows { get; set; }

        // Review & Report
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Report> Reports { get; set; }

        // Community
        public DbSet<CommunityPost> CommunityPosts { get; set; }
        public DbSet<CommunityComment> CommunityComments { get; set; }
        public DbSet<CommunityPostHashtag> CommunityPostHashtags { get; set; }
        public DbSet<CommunityPostFile> CommunityPostFiles { get; set; }

        // Follow
        public DbSet<UserFollow> UserFollows { get; set; }

        // Verification Tokens
        public DbSet<VerificationToken> VerificationTokens { get; set; }

        // Mentor Application History (for audit trail and ML training)
        public DbSet<MentorApplicationHistory> MentorApplicationHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map table names to existing snake_case names (match migrations / SQL Server)
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<UserProfile>().ToTable("user_profiles");
            modelBuilder.Entity<MentorProfile>().ToTable("mentor_profiles");
            modelBuilder.Entity<Notification>().ToTable("notifications");

            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<Hashtag>().ToTable("hashtags");
            modelBuilder.Entity<MentorCategory>().ToTable("mentor_categories");
            modelBuilder.Entity<MentorHashtag>().ToTable("mentor_hashtags");
            modelBuilder.Entity<CategoryHashtag>().ToTable("category_hashtags");

            modelBuilder.Entity<Request>().ToTable("requests");
            modelBuilder.Entity<Proposal>().ToTable("proposals");
            modelBuilder.Entity<Order>().ToTable("orders");
            modelBuilder.Entity<OrderDetail>().ToTable("order_details");

            modelBuilder.Entity<WorkSession>().ToTable("work_sessions");
            modelBuilder.Entity<SessionTracking>().ToTable("session_tracking");

            modelBuilder.Entity<Conversation>().ToTable("conversations");
            modelBuilder.Entity<Message>().ToTable("messages");
            modelBuilder.Entity<MessageFile>().ToTable("message_files");

            modelBuilder.Entity<Wallet>().ToTable("wallets");
            modelBuilder.Entity<Transaction>().ToTable("transactions");
            modelBuilder.Entity<Escrow>().ToTable("escrows");

            modelBuilder.Entity<Review>().ToTable("reviews");
            modelBuilder.Entity<Report>().ToTable("reports");

            modelBuilder.Entity<CommunityPost>().ToTable("community_posts");
            modelBuilder.Entity<CommunityComment>().ToTable("community_comments");
            modelBuilder.Entity<CommunityPostHashtag>().ToTable("community_post_hashtags");
            modelBuilder.Entity<CommunityPostFile>().ToTable("community_post_files");
            modelBuilder.Entity<UserFollow>().ToTable("user_follows");
            modelBuilder.Entity<VerificationToken>().ToTable("verification_tokens");
            modelBuilder.Entity<MentorApplicationHistory>().ToTable("mentor_application_history");

            // User - Unique Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure decimal precision/column types to match SQL Server migration
            modelBuilder.Entity<MentorProfile>()
                .Property(mp => mp.HourlyRate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MentorProfile>()
                .Property(mp => mp.PackagePrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MentorProfile>()
                .Property(mp => mp.RatingAvg)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.AgreedPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.AgreedHourlyRate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Proposal>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Proposal>()
                .Property(p => p.EstimatedHours)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Request>()
                .Property(r => r.ExpectedBudget)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Request>()
                .Property(r => r.ExpectedHours)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Wallet>()
                .Property(w => w.LockedBalance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Escrow>()
                .Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            // Notification.Data store as nvarchar(max) (SQL Server does not have native json type)
            modelBuilder.Entity<Notification>()
                .Property(n => n.Data)
                .HasColumnType("nvarchar(max)");

            // MentorApplicationHistory configuration
            modelBuilder.Entity<MentorApplicationHistory>()
                .Property(h => h.ApplicationSnapshot)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<MentorApplicationHistory>()
                .Property(h => h.AiConfidenceScore)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<MentorApplicationHistory>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MentorApplicationHistory>()
                .HasOne(h => h.ActionPerformedBy)
                .WithMany()
                .HasForeignKey(h => h.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MentorApplicationHistory>()
                .HasIndex(h => h.UserId);

            modelBuilder.Entity<MentorApplicationHistory>()
                .HasIndex(h => h.Action);

            modelBuilder.Entity<MentorApplicationHistory>()
                .HasIndex(h => h.CreatedAt);

            // UserProfile - One-to-One with User
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // MentorProfile - One-to-One with User
            modelBuilder.Entity<MentorProfile>()
                .HasOne(mp => mp.User)
                .WithOne(u => u.MentorProfile)
                .HasForeignKey<MentorProfile>(mp => mp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Wallet - One-to-One with User
            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId)
                .IsUnique();

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne(u => u.Wallet)
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // MentorCategory - Composite Key
            modelBuilder.Entity<MentorCategory>()
                .HasKey(mc => new { mc.MentorId, mc.CategoryId });

            modelBuilder.Entity<MentorCategory>()
                .HasOne(mc => mc.Mentor)
                .WithMany(u => u.MentorCategories)
                .HasForeignKey(mc => mc.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MentorCategory>()
                .HasOne(mc => mc.Category)
                .WithMany(c => c.MentorCategories)
                .HasForeignKey(mc => mc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // MentorHashtag - Composite Key
            modelBuilder.Entity<MentorHashtag>()
                .HasKey(mh => new { mh.MentorId, mh.HashtagId });

            modelBuilder.Entity<MentorHashtag>()
                .HasOne(mh => mh.Mentor)
                .WithMany(u => u.MentorHashtags)
                .HasForeignKey(mh => mh.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MentorHashtag>()
                .HasOne(mh => mh.Hashtag)
                .WithMany(h => h.MentorHashtags)
                .HasForeignKey(mh => mh.HashtagId)
                .OnDelete(DeleteBehavior.Cascade);

            // CategoryHashtag - Composite Key (Many-to-Many mapping)
            modelBuilder.Entity<CategoryHashtag>()
                .HasKey(ch => new { ch.CategoryId, ch.HashtagId });

            modelBuilder.Entity<CategoryHashtag>()
                .HasOne(ch => ch.Category)
                .WithMany(c => c.CategoryHashtags)
                .HasForeignKey(ch => ch.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CategoryHashtag>()
                .HasOne(ch => ch.Hashtag)
                .WithMany(h => h.CategoryHashtags)
                .HasForeignKey(ch => ch.HashtagId)
                .OnDelete(DeleteBehavior.Cascade);

            // Hashtag - Unique Name
            modelBuilder.Entity<Hashtag>()
                .HasIndex(h => h.Name)
                .IsUnique();

            // Category - Self-referencing
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Request
            modelBuilder.Entity<Request>()
                .HasOne(r => r.Student)
                .WithMany(u => u.Requests)
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Proposal
            modelBuilder.Entity<Proposal>()
                .HasOne(p => p.Mentor)
                .WithMany(u => u.Proposals)
                .HasForeignKey(p => p.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Proposal>()
                .HasOne(p => p.Request)
                .WithMany(r => r.Proposals)
                .HasForeignKey(p => p.RequestId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Proposal>()
                .HasOne(p => p.Post)
                .WithMany(cp => cp.Proposals)
                .HasForeignKey(p => p.PostId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Order - Multiple FKs to User
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Student)
                .WithMany(u => u.StudentOrders)
                .HasForeignKey(o => o.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Mentor)
                .WithMany(u => u.MentorOrders)
                .HasForeignKey(o => o.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Request)
                .WithMany(r => r.Orders)
                .HasForeignKey(o => o.RequestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Proposal)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.ProposalId)
                .OnDelete(DeleteBehavior.NoAction);

            // Escrow - One-to-One with Order
            modelBuilder.Entity<Escrow>()
                .HasIndex(e => e.OrderId)
                .IsUnique();

            modelBuilder.Entity<Escrow>()
                .HasOne(e => e.Order)
                .WithOne(o => o.Escrow)
                .HasForeignKey<Escrow>(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkSession
            modelBuilder.Entity<WorkSession>()
                .HasOne(ws => ws.Mentor)
                .WithMany()
                .HasForeignKey(ws => ws.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkSession>()
                .HasOne(ws => ws.Student)
                .WithMany()
                .HasForeignKey(ws => ws.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Conversation
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Mentor)
                .WithMany()
                .HasForeignKey(c => c.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review
            modelBuilder.Entity<Review>()
                .HasOne(r => r.FromUser)
                .WithMany(u => u.ReviewsGiven)
                .HasForeignKey(r => r.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.ToUser)
                .WithMany(u => u.ReviewsReceived)
                .HasForeignKey(r => r.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Report
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.ReportsCreated)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ReportedUser)
                .WithMany(u => u.ReportsReceived)
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Handler)
                .WithMany()
                .HasForeignKey(r => r.HandledBy)
                .OnDelete(DeleteBehavior.Restrict);

            // CommunityPost
            modelBuilder.Entity<CommunityPost>()
                .HasOne(cp => cp.Author)
                .WithMany(u => u.CommunityPosts)
                .HasForeignKey(cp => cp.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityPost>()
                .HasOne(cp => cp.Category)
                .WithMany()
                .HasForeignKey(cp => cp.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // CommunityPostHashtag - Composite Key
            modelBuilder.Entity<CommunityPostHashtag>()
                .HasKey(cph => new { cph.PostId, cph.HashtagId });

            modelBuilder.Entity<CommunityPostHashtag>()
                .HasOne(cph => cph.Post)
                .WithMany(p => p.PostHashtags)
                .HasForeignKey(cph => cph.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityPostHashtag>()
                .HasOne(cph => cph.Hashtag)
                .WithMany(h => h.CommunityPostHashtags)
                .HasForeignKey(cph => cph.HashtagId)
                .OnDelete(DeleteBehavior.Cascade);

            // CommunityPostFile
            modelBuilder.Entity<CommunityPostFile>()
                .HasOne(cpf => cpf.Post)
                .WithMany(p => p.Files)
                .HasForeignKey(cpf => cpf.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // CommunityComment
            modelBuilder.Entity<CommunityComment>()
                .HasOne(cc => cc.Author)
                .WithMany(u => u.CommunityComments)
                .HasForeignKey(cc => cc.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunityComment>()
                .HasOne(cc => cc.Post)
                .WithMany(cp => cp.Comments)
                .HasForeignKey(cc => cc.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserFollow
            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollow>()
                .HasIndex(uf => new { uf.FollowerId, uf.FollowingId })
                .IsUnique();

            // OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.ChangedByUser)
                .WithMany()
                .HasForeignKey(od => od.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Order)
                .WithMany(o => o.Transactions)
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Store enums as strings
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Request>()
                .Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Proposal>()
                .Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Order>()
                .Property(o => o.BillingType)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<WorkSession>()
                .Property(ws => ws.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Message>()
                .Property(m => m.MessageType)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Escrow>()
                .Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}

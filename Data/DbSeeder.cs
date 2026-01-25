using BCrypt.Net;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Data
{
    public static class DbSeeder
    {
        private const string AdminEmail = "admin@mentor-edu.local";
        private const string StudentEmail = "student@mentor-edu.local";
        private const string MentorEmail = "mentor@mentor-edu.local";

        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.AppUsers.AnyAsync(u => u.Email == AdminEmail || u.Email == StudentEmail || u.Email == MentorEmail))
            {
                return;
            }

            var now = DateTime.UtcNow;

            var admin = new User
            {
                Email = AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-1)
            };

            var student = new User
            {
                Email = StudentEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student123!"),
                Role = UserRole.Student,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-2)
            };

            var mentor = new User
            {
                Email = MentorEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Mentor123!"),
                Role = UserRole.Mentor,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now.AddDays(-2)
            };

            await context.AppUsers.AddRangeAsync(admin, student, mentor);
            await context.SaveChangesAsync();

            var adminProfile = new UserProfile
            {
                UserId = admin.Id,
                FullName = "System Admin",
                City = "Ho Chi Minh City",
                Country = "Vietnam",
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-1)
            };

            var studentProfile = new UserProfile
            {
                UserId = student.Id,
                FullName = "Nguyen Student",
                School = "Mentor Edu University",
                Major = "Computer Science",
                City = "Da Nang",
                Country = "Vietnam",
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-2)
            };

            var mentorProfile = new UserProfile
            {
                UserId = mentor.Id,
                FullName = "Tran Mentor",
                City = "Ha Noi",
                Country = "Vietnam",
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now.AddDays(-2)
            };

            var mentorExtra = new MentorProfile
            {
                UserId = mentor.Id,
                Title = "Senior .NET Mentor",
                HourlyRate = 250000,
                PackagePrice = 1500000,
                ExperienceYears = 6,
                Introduction = "Helping students build real-world .NET projects.",
                AvailabilityNote = "Weekends & evenings",
                RatingAvg = 4.8m,
                RatingCount = 12,
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now.AddDays(-2)
            };

            await context.UserProfiles.AddRangeAsync(adminProfile, studentProfile, mentorProfile);
            await context.MentorProfiles.AddAsync(mentorExtra);

            var adminWallet = new Wallet
            {
                UserId = admin.Id,
                Balance = 0,
                LockedBalance = 0,
                Currency = "VND",
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-1)
            };

            var studentWallet = new Wallet
            {
                UserId = student.Id,
                Balance = 2000000,
                LockedBalance = 500000,
                Currency = "VND",
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-1)
            };

            var mentorWallet = new Wallet
            {
                UserId = mentor.Id,
                Balance = 1500000,
                LockedBalance = 0,
                Currency = "VND",
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now.AddDays(-1)
            };

            await context.Wallets.AddRangeAsync(adminWallet, studentWallet, mentorWallet);

            var rootCategory = new Category
            {
                Name = "Software Development",
                Description = "Programming, architecture, and code reviews"
            };

            await context.Categories.AddAsync(rootCategory);

            var hashtagDotnet = new Hashtag
            {
                Name = "dotnet"
            };

            var hashtagBackend = new Hashtag
            {
                Name = "backend"
            };

            await context.Hashtags.AddRangeAsync(hashtagDotnet, hashtagBackend);
            await context.SaveChangesAsync();

            var mentorCategory = new MentorCategory
            {
                MentorId = mentor.Id,
                CategoryId = rootCategory.Id
            };

            var mentorHashtag1 = new MentorHashtag
            {
                MentorId = mentor.Id,
                HashtagId = hashtagDotnet.Id
            };

            var mentorHashtag2 = new MentorHashtag
            {
                MentorId = mentor.Id,
                HashtagId = hashtagBackend.Id
            };

            await context.MentorCategories.AddAsync(mentorCategory);
            await context.MentorHashtags.AddRangeAsync(mentorHashtag1, mentorHashtag2);

            var request = new Request
            {
                StudentId = student.Id,
                Title = "Cần mentor review API .NET",
                Description = "Muốn mentor góp ý cấu trúc API và tối ưu query.",
                CategoryId = rootCategory.Id,
                ExpectedBudget = 2000000,
                ExpectedHours = 8,
                Status = RequestStatus.Matched,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-9)
            };

            await context.Requests.AddAsync(request);
            await context.SaveChangesAsync();

            var proposal = new Proposal
            {
                RequestId = request.Id,
                MentorId = mentor.Id,
                Price = 1800000,
                EstimatedHours = 8,
                Message = "Mình có thể hỗ trợ review kiến trúc + tối ưu hiệu năng.",
                Status = ProposalStatus.Accepted,
                CreatedAt = now.AddDays(-9),
                UpdatedAt = now.AddDays(-9)
            };

            await context.Proposals.AddAsync(proposal);
            await context.SaveChangesAsync();

            var order = new Order
            {
                RequestId = request.Id,
                ProposalId = proposal.Id,
                StudentId = student.Id,
                MentorId = mentor.Id,
                BillingType = BillingType.Fixed,
                AgreedPrice = 1800000,
                TotalPrice = 1800000,
                Status = OrderStatus.InProgress,
                StartedAt = now.AddDays(-7),
                CreatedAt = now.AddDays(-8),
                UpdatedAt = now.AddDays(-7)
            };

            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();

            var orderDetail = new OrderDetail
            {
                OrderId = order.Id,
                OldStatus = OrderStatus.PendingPayment.ToString(),
                NewStatus = OrderStatus.InProgress.ToString(),
                ChangedBy = student.Id,
                Note = "Student đã thanh toán và bắt đầu làm việc.",
                CreatedAt = now.AddDays(-7)
            };

            await context.OrderDetails.AddAsync(orderDetail);

            var workSession = new WorkSession
            {
                OrderId = order.Id,
                MentorId = mentor.Id,
                StudentId = student.Id,
                StartTime = now.AddDays(-6).AddHours(-2),
                EndTime = now.AddDays(-6),
                TotalMinutes = 120,
                Status = WorkSessionStatus.Finished,
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-6)
            };

            await context.WorkSessions.AddAsync(workSession);
            await context.SaveChangesAsync();

            var sessionTracking = new SessionTracking
            {
                SessionId = workSession.Id,
                StartAt = workSession.StartTime,
                EndAt = workSession.EndTime,
                DurationMin = workSession.TotalMinutes
            };

            await context.SessionTrackings.AddAsync(sessionTracking);

            var conversation = new Conversation
            {
                OrderId = order.Id,
                StudentId = student.Id,
                MentorId = mentor.Id,
                CreatedAt = now.AddDays(-7)
            };

            await context.Conversations.AddAsync(conversation);
            await context.SaveChangesAsync();

            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = mentor.Id,
                Content = "Mình đã upload file checklist review.",
                MessageType = MessageType.File,
                IsRead = false,
                CreatedAt = now.AddDays(-7)
            };

            await context.Messages.AddAsync(message);
            await context.SaveChangesAsync();

            var messageFile = new MessageFile
            {
                MessageId = message.Id,
                FileUrl = "https://example.com/files/review-checklist.pdf",
                FileType = "application/pdf",
                FileName = "review-checklist.pdf",
                FileSize = 245760
            };

            await context.MessageFiles.AddAsync(messageFile);

            var escrow = new Escrow
            {
                OrderId = order.Id,
                Amount = 1800000,
                Status = EscrowStatus.Holding,
                CreatedAt = now.AddDays(-8)
            };

            await context.Escrows.AddAsync(escrow);

            var studentTopup = new Transaction
            {
                WalletId = studentWallet.Id,
                OrderId = null,
                Type = TransactionType.Topup,
                Amount = 2000000,
                ExternalRef = "TOPUP-001",
                Status = TransactionStatus.Success,
                Description = "Nạp tiền ví để thanh toán đơn.",
                CreatedAt = now.AddDays(-9)
            };

            var escrowHold = new Transaction
            {
                WalletId = studentWallet.Id,
                OrderId = order.Id,
                Type = TransactionType.EscrowHold,
                Amount = 1800000,
                ExternalRef = "ESCROW-001",
                Status = TransactionStatus.Success,
                Description = "Giữ tiền vào escrow cho order.",
                CreatedAt = now.AddDays(-8)
            };

            var mentorRelease = new Transaction
            {
                WalletId = mentorWallet.Id,
                OrderId = order.Id,
                Type = TransactionType.EscrowRelease,
                Amount = 1800000,
                ExternalRef = "ESCROW-REL-001",
                Status = TransactionStatus.Pending,
                Description = "Chuẩn bị giải ngân cho mentor.",
                CreatedAt = now.AddDays(-7)
            };

            await context.Transactions.AddRangeAsync(studentTopup, escrowHold, mentorRelease);

            var review = new Review
            {
                OrderId = order.Id,
                FromUserId = student.Id,
                ToUserId = mentor.Id,
                Rating = 5,
                Comment = "Mentor hỗ trợ rất chi tiết và dễ hiểu.",
                CreatedAt = now.AddDays(-5)
            };

            await context.Reviews.AddAsync(review);

            var report = new Report
            {
                ReporterId = student.Id,
                ReportedUserId = mentor.Id,
                OrderId = order.Id,
                Reason = "Cần làm rõ phạm vi deliverable.",
                Status = "Open",
                HandledBy = admin.Id,
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-3)
            };

            await context.Reports.AddAsync(report);

            var communityPost = new CommunityPost
            {
                AuthorId = student.Id,
                Title = "Kinh nghiệm làm việc với mentor",
                Content = "Mình vừa hoàn thành buổi mentor về API .NET.",
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-3)
            };

            await context.CommunityPosts.AddAsync(communityPost);
            await context.SaveChangesAsync();

            var communityComment = new CommunityComment
            {
                PostId = communityPost.Id,
                AuthorId = mentor.Id,
                Content = "Cảm ơn bạn đã chia sẻ trải nghiệm!",
                CreatedAt = now.AddDays(-2)
            };

            await context.CommunityComments.AddAsync(communityComment);

            var adminNotification = new Notification
            {
                UserId = admin.Id,
                Type = NotificationType.System,
                Title = "New report assigned",
                Body = "A report is awaiting your review.",
                Data = "{\"reportId\":" + report.Id + "}",
                CreatedAt = now.AddDays(-3)
            };

            var studentNotification = new Notification
            {
                UserId = student.Id,
                Type = NotificationType.OrderStarted,
                Title = "Order started",
                Body = "Mentor has started working on your order.",
                Data = "{\"orderId\":" + order.Id + "}",
                CreatedAt = now.AddDays(-7)
            };

            var mentorNotification = new Notification
            {
                UserId = mentor.Id,
                Type = NotificationType.ProposalAccepted,
                Title = "Proposal accepted",
                Body = "Student accepted your proposal.",
                Data = "{\"proposalId\":" + proposal.Id + "}",
                CreatedAt = now.AddDays(-8)
            };

            await context.Notifications.AddRangeAsync(adminNotification, studentNotification, mentorNotification);

            await context.SaveChangesAsync();
        }
    }
}
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
        private const string DefaultPassword = "Password123!";

        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Only seed if no main accounts exist
            if (await context.AppUsers.AnyAsync(u => u.Email == AdminEmail))
                return;

            var now = DateTime.UtcNow;
            var adminHash   = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            var studentHash = BCrypt.Net.BCrypt.HashPassword("Student123!");
            var mentorHash  = BCrypt.Net.BCrypt.HashPassword("Mentor123!");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);

            // ─────────── 1. CATEGORIES ───────────
            var categories = new[]
            {
                new Category { Name = "Web Development", Description = "Frontend, backend, full-stack web technologies" },
                new Category { Name = "Mobile Development", Description = "iOS, Android, React Native, Flutter" },
                new Category { Name = "Data Science", Description = "Machine learning, data analysis, AI" },
                new Category { Name = "DevOps & Cloud", Description = "AWS, Azure, Docker, Kubernetes, CI/CD" },
                new Category { Name = "Design & UI/UX", Description = "Figma, Adobe XD, user research, prototyping" },
                new Category { Name = "Database", Description = "SQL, NoSQL, database design and optimization" },
                new Category { Name = "Cybersecurity", Description = "Security, penetration testing, compliance" },
                new Category { Name = "Programming Languages", Description = "C#, Java, Python, JavaScript, Go, Rust" },
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            // ─────────── 2. HASHTAGS ───────────
            var hashtagNames = new[]
            {
                "react", "angular", "vue", "nextjs", "dotnet", "csharp", "java", "python",
                "javascript", "typescript", "nodejs", "expressjs", "flutter", "reactnative",
                "swift", "kotlin", "docker", "kubernetes", "aws", "azure", "sql", "mongodb",
                "postgresql", "redis", "git", "cicd", "figma", "tailwindcss", "bootstrap",
                "graphql", "restapi", "microservices", "machinelearning", "deeplearning",
                "datascience", "cybersecurity", "blockchain", "golang", "rust", "linux"
            };
            var hashtags = hashtagNames.Select(n => new Hashtag { Name = n }).ToArray();
            await context.Hashtags.AddRangeAsync(hashtags);
            await context.SaveChangesAsync();

            // ─────────── 3. CATEGORY ↔ HASHTAG MAPPING ───────────
            var catHashMap = new Dictionary<string, string[]>
            {
                ["Web Development"] = new[] { "react", "angular", "vue", "nextjs", "javascript", "typescript", "nodejs", "expressjs", "tailwindcss", "bootstrap", "graphql", "restapi" },
                ["Mobile Development"] = new[] { "flutter", "reactnative", "swift", "kotlin", "javascript", "typescript" },
                ["Data Science"] = new[] { "python", "machinelearning", "deeplearning", "datascience", "sql", "mongodb" },
                ["DevOps & Cloud"] = new[] { "docker", "kubernetes", "aws", "azure", "cicd", "linux", "git" },
                ["Design & UI/UX"] = new[] { "figma", "tailwindcss", "bootstrap" },
                ["Database"] = new[] { "sql", "mongodb", "postgresql", "redis" },
                ["Cybersecurity"] = new[] { "cybersecurity", "linux" },
                ["Programming Languages"] = new[] { "csharp", "java", "python", "javascript", "typescript", "golang", "rust", "dotnet" },
            };
            var catLookup = categories.ToDictionary(c => c.Name!);
            var hashLookup = hashtags.ToDictionary(h => h.Name);
            var catHashtags = new List<CategoryHashtag>();
            foreach (var (catName, tags) in catHashMap)
            {
                foreach (var tag in tags)
                    if (catLookup.ContainsKey(catName) && hashLookup.ContainsKey(tag))
                        catHashtags.Add(new CategoryHashtag { CategoryId = catLookup[catName].Id, HashtagId = hashLookup[tag].Id });
            }
            await context.CategoryHashtags.AddRangeAsync(catHashtags);
            await context.SaveChangesAsync();

            // ─────────── 4. USERS: ADMIN, STUDENT, MENTOR ───────────
            var admin = new User
            {
                Email = AdminEmail,
                PasswordHash = adminHash,
                Role = UserRole.Admin,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now
            };
            var student = new User
            {
                Email = StudentEmail,
                PasswordHash = studentHash,
                Role = UserRole.Student,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-45),
                UpdatedAt = now
            };
            var mentor = new User
            {
                Email = MentorEmail,
                PasswordHash = mentorHash,
                Role = UserRole.Mentor,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-50),
                UpdatedAt = now
            };
            await context.AppUsers.AddRangeAsync(admin, student, mentor);
            await context.SaveChangesAsync();

            // ─────────── 5. USER PROFILES ───────────
            var profiles = new[]
            {
                new UserProfile
                {
                    UserId = admin.Id,
                    FullName = "Admin EduMentor",
                    Phone = "0901000000",
                    Gender = "Male",
                    School = "EduMentor Platform",
                    Major = "System Administration",
                    Bio = "Platform administrator",
                    City = "Ho Chi Minh",
                    Country = "Vietnam",
                    CreatedAt = now, UpdatedAt = now
                },
                new UserProfile
                {
                    UserId = student.Id,
                    FullName = "Nguyễn Văn Sinh Viên",
                    Phone = "0902000000",
                    Gender = "Male",
                    School = "Đại học Bách Khoa TP.HCM",
                    Major = "Computer Science",
                    Bio = "Sinh viên năm 3, đam mê lập trình web và mobile. Đang tìm mentor hướng dẫn dự án tốt nghiệp.",
                    City = "Ho Chi Minh",
                    Country = "Vietnam",
                    CreatedAt = now, UpdatedAt = now
                },
                new UserProfile
                {
                    UserId = mentor.Id,
                    FullName = "Trần Minh Mentor",
                    Phone = "0903000000",
                    Gender = "Male",
                    School = "Đại học Khoa học Tự nhiên",
                    Major = "Software Engineering",
                    Bio = "Senior Full-Stack Developer với 8 năm kinh nghiệm. Chuyên React, .NET, và Cloud Architecture.",
                    City = "Ho Chi Minh",
                    Country = "Vietnam",
                    CreatedAt = now, UpdatedAt = now
                }
            };
            await context.UserProfiles.AddRangeAsync(profiles);
            await context.SaveChangesAsync();

            // ─────────── 6. MENTOR PROFILE (main mentor) ───────────
            var mentorProfile = new MentorProfile
            {
                UserId = mentor.Id,
                Title = "Senior Full-Stack Developer",
                HourlyRate = 200_000,
                PackagePrice = 1_500_000,
                ExperienceYears = 8,
                Introduction = "Xin chào! Mình là full-stack developer với 8 năm kinh nghiệm làm việc tại các công ty tech lớn. Mình chuyên về React, .NET Core, và cloud computing. Mình sẽ giúp bạn từ cơ bản đến nâng cao.",
                AvailabilityNote = "Có thể mentor vào buổi tối các ngày trong tuần và cả ngày cuối tuần",
                RatingAvg = 4.8m,
                RatingCount = 12,
                ApprovalStatus = MentorApprovalStatus.Approved,
                Specialization = "Full-Stack Web Development, Cloud Architecture",
                PortfolioUrl = "https://github.com/trminh-mentor",
                ReviewedAt = now.AddDays(-40),
                CreatedAt = now.AddDays(-50),
                UpdatedAt = now
            };
            await context.MentorProfiles.AddAsync(mentorProfile);
            await context.SaveChangesAsync();

            // ─────────── 7. TEN ADDITIONAL MENTORS ───────────
            var mentorData = new[]
            {
                ("mentor01@mentor-edu.local", "Lê Thị Hương", "Female", "React & Frontend Expert", 150_000m, 1_200_000m, 5, 4.9m, 20, "ĐH FPT", "Chuyên gia Frontend với 5 năm kinh nghiệm, đặc biệt về React và TypeScript.", new[]{"react","typescript","nextjs","tailwindcss"}, new[]{"Web Development"}),
                ("mentor02@mentor-edu.local", "Phạm Quốc Bảo", "Male", "Backend .NET Specialist", 180_000m, 1_400_000m, 6, 4.7m, 15, "ĐH Bách Khoa", "Senior .NET developer, chuyên microservices và system design.", new[]{"dotnet","csharp","sql","restapi","microservices"}, new[]{"Web Development","Database"}),
                ("mentor03@mentor-edu.local", "Nguyễn Hoài An", "Female", "Mobile Developer", 160_000m, 1_300_000m, 4, 4.6m, 8, "ĐH CNTT", "Mobile dev chuyên Flutter và React Native, đã publish 10+ apps trên store.", new[]{"flutter","reactnative","javascript","kotlin"}, new[]{"Mobile Development"}),
                ("mentor04@mentor-edu.local", "Trương Đức Huy", "Male", "Data Scientist", 250_000m, 2_000_000m, 7, 4.8m, 18, "ĐH Khoa học Tự nhiên", "Data Scientist tại VNG, chuyên ML/DL và NLP. Kinh nghiệm Kaggle top 1%.", new[]{"python","machinelearning","deeplearning","datascience","sql"}, new[]{"Data Science"}),
                ("mentor05@mentor-edu.local", "Võ Thanh Tùng", "Male", "DevOps Engineer", 220_000m, 1_800_000m, 6, 4.5m, 10, "ĐH Bách Khoa Đà Nẵng", "DevOps engineer chuyên AWS, Docker, Kubernetes. Giúp bạn xây dựng CI/CD pipeline.", new[]{"docker","kubernetes","aws","cicd","linux","git"}, new[]{"DevOps & Cloud"}),
                ("mentor06@mentor-edu.local", "Đặng Thị Mai", "Female", "UI/UX Designer", 140_000m, 1_000_000m, 4, 4.9m, 22, "ĐH Mỹ Thuật", "Lead designer tại Tiki, chuyên user research và design system.", new[]{"figma","tailwindcss","bootstrap"}, new[]{"Design & UI/UX"}),
                ("mentor07@mentor-edu.local", "Bùi Minh Đức", "Male", "Java Backend Developer", 190_000m, 1_500_000m, 7, 4.7m, 14, "ĐH Công nghệ", "Senior Java developer tại Shopee, chuyên Spring Boot và distributed systems.", new[]{"java","sql","postgresql","microservices","docker","restapi"}, new[]{"Programming Languages","Database"}),
                ("mentor08@mentor-edu.local", "Lý Ngọc Trâm", "Female", "Python Full-Stack", 170_000m, 1_300_000m, 5, 4.6m, 11, "ĐH FPT", "Python developer chuyên Django/FastAPI và data engineering.", new[]{"python","javascript","postgresql","mongodb","restapi","graphql"}, new[]{"Web Development","Data Science"}),
                ("mentor09@mentor-edu.local", "Hoàng Văn Nam", "Male", "Cybersecurity Expert", 280_000m, 2_200_000m, 9, 4.8m, 7, "ĐH An ninh", "Pentester & security consultant, OSCP certified.", new[]{"cybersecurity","linux","python"}, new[]{"Cybersecurity"}),
                ("mentor10@mentor-edu.local", "Phan Thị Yến Nhi", "Female", "Cloud Architect", 260_000m, 2_000_000m, 8, 4.7m, 16, "ĐH Bách Khoa", "AWS Solutions Architect chuyên thiết kế hệ thống cloud-native.", new[]{"aws","azure","docker","kubernetes","cicd","microservices"}, new[]{"DevOps & Cloud","Web Development"}),
            };

            var mentorUsers = new List<User>();
            foreach (var (email, fullName, gender, title, hourly, package, exp, rating, ratingCount, school, intro, tags, cats) in mentorData)
            {
                var u = new User
                {
                    Email = email,
                    PasswordHash = passwordHash,
                    Role = UserRole.Mentor,
                    IsVerified = true,
                    IsActive = true,
                    CreatedAt = now.AddDays(-Random.Shared.Next(30, 90)),
                    UpdatedAt = now
                };
                await context.AppUsers.AddAsync(u);
                await context.SaveChangesAsync();

                await context.UserProfiles.AddAsync(new UserProfile
                {
                    UserId = u.Id,
                    FullName = fullName,
                    Phone = $"090{Random.Shared.Next(1000000, 9999999)}",
                    Gender = gender,
                    School = school,
                    Major = "Computer Science",
                    Bio = intro,
                    City = Random.Shared.Next(2) == 0 ? "Ho Chi Minh" : "Ha Noi",
                    Country = "Vietnam",
                    CreatedAt = now, UpdatedAt = now
                });

                await context.MentorProfiles.AddAsync(new MentorProfile
                {
                    UserId = u.Id,
                    Title = title,
                    HourlyRate = hourly,
                    PackagePrice = package,
                    ExperienceYears = exp,
                    Introduction = intro,
                    AvailabilityNote = "Có thể dạy buổi tối và cuối tuần",
                    RatingAvg = rating,
                    RatingCount = ratingCount,
                    ApprovalStatus = MentorApprovalStatus.Approved,
                    Specialization = title,
                    ReviewedAt = now.AddDays(-20),
                    CreatedAt = now.AddDays(-30),
                    UpdatedAt = now
                });

                // Mentor ↔ Category
                foreach (var catName in cats)
                    if (catLookup.ContainsKey(catName))
                        await context.MentorCategories.AddAsync(new MentorCategory { MentorId = u.Id, CategoryId = catLookup[catName].Id });

                // Mentor ↔ Hashtag
                foreach (var tag in tags)
                    if (hashLookup.ContainsKey(tag))
                        await context.MentorHashtags.AddAsync(new MentorHashtag { MentorId = u.Id, HashtagId = hashLookup[tag].Id });

                await context.SaveChangesAsync();
                mentorUsers.Add(u);
            }

            // Mentor categories/hashtags for the main mentor
            var mainMentorCats = new[] { "Web Development", "Programming Languages" };
            var mainMentorTags = new[] { "react", "dotnet", "csharp", "typescript", "javascript", "nextjs", "restapi" };
            foreach (var c in mainMentorCats)
                if (catLookup.ContainsKey(c))
                    await context.MentorCategories.AddAsync(new MentorCategory { MentorId = mentor.Id, CategoryId = catLookup[c].Id });
            foreach (var t in mainMentorTags)
                if (hashLookup.ContainsKey(t))
                    await context.MentorHashtags.AddAsync(new MentorHashtag { MentorId = mentor.Id, HashtagId = hashLookup[t].Id });
            await context.SaveChangesAsync();

            // ─────────── 8. WALLETS ───────────
            var allUsers = new List<User> { admin, student, mentor };
            allUsers.AddRange(mentorUsers);
            foreach (var u in allUsers)
            {
                var balance = u.Role == UserRole.Student ? 1_000_000m
                            : u.Role == UserRole.Mentor ? 500_000m
                            : 0m;
                await context.Wallets.AddAsync(new Wallet
                {
                    UserId = u.Id,
                    Balance = balance,
                    LockedBalance = 0,
                    Currency = "VND",
                    CreatedAt = now, UpdatedAt = now
                });
            }
            await context.SaveChangesAsync();

            // ─────────── 9. COMMUNITY POSTS (20 posts) ───────────
            var allMentors = new List<User> { mentor };
            allMentors.AddRange(mentorUsers);
            var postAuthors = new List<User> { student };
            postAuthors.AddRange(allMentors);

            var postTemplates = new[]
            {
                (catName: "Web Development", title: "Hướng dẫn React từ cơ bản đến nâng cao", content: "React là thư viện JavaScript phổ biến nhất hiện nay. Trong bài viết này mình sẽ chia sẻ lộ trình học React từ cơ bản (JSX, Components, Props, State) đến nâng cao (Hooks, Context, Redux, React Query). Ai đang học React thì cùng thảo luận nhé!", tags: new[]{"react","javascript","typescript"}),
                (catName: "Web Development", title: "So sánh Next.js vs Vite + React", content: "Mình đã thử cả 2 stack cho dự án mới. Next.js mạnh về SSR, SEO, routing tích hợp. Vite + React thì nhanh, nhẹ, linh hoạt hơn. Tùy project mà chọn. Các bạn dùng stack nào cho dự án của mình?", tags: new[]{"nextjs","react","javascript"}),
                (catName: "Programming Languages", title: "Tại sao nên học C# và .NET trong 2026?", content: ".NET 9 đã ra mắt với nhiều cải tiến performance đáng kinh ngạc. C# ngày càng modern với pattern matching, record types, và AOT compilation. Ecosystem .NET rất mạnh cho enterprise, game (Unity), và cloud. Bạn nào đang cân nhắc học thì đây là thời điểm tốt!", tags: new[]{"csharp","dotnet","restapi"}),
                (catName: "Mobile Development", title: "Flutter vs React Native 2026 - Nên chọn cái nào?", content: "Mình đã dùng cả 2 framework cho các dự án thực tế. Flutter mạnh về UI consistency và performance. React Native mạnh về ecosystem và code sharing với web. Chia sẻ kinh nghiệm thực chiến của mình.", tags: new[]{"flutter","reactnative","javascript"}),
                (catName: "Data Science", title: "Lộ trình học Machine Learning cho người mới", content: "Bước 1: Học Python cơ bản → Bước 2: Numpy, Pandas → Bước 3: Scikit-learn → Bước 4: Deep Learning (TensorFlow/PyTorch) → Bước 5: Làm project thực tế. Mỗi bước mình sẽ recommend tài liệu cụ thể.", tags: new[]{"python","machinelearning","datascience"}),
                (catName: "DevOps & Cloud", title: "Docker cho người mới bắt đầu", content: "Docker giúp đóng gói ứng dụng vào container, chạy được ở mọi nơi. Bài viết này mình hướng dẫn: Dockerfile, docker-compose, networking, volumes. Cuối bài có demo containerize một ứng dụng .NET + PostgreSQL.", tags: new[]{"docker","kubernetes","linux"}),
                (catName: "Design & UI/UX", title: "Thiết kế UI/UX hiệu quả với Figma", content: "Figma là công cụ design được yêu thích nhất hiện nay. Tips của mình: 1) Sử dụng Auto Layout, 2) Tạo Design System, 3) Component variants, 4) Prototyping. Ai cần template Figma mình share nhé!", tags: new[]{"figma","tailwindcss"}),
                (catName: "Database", title: "PostgreSQL vs MongoDB - Khi nào dùng cái nào?", content: "PostgreSQL cho dữ liệu có cấu trúc, cần ACID transactions, complex queries. MongoDB cho dữ liệu linh hoạt, horizontal scaling, real-time analytics. Mình sẽ phân tích từng use case cụ thể.", tags: new[]{"postgresql","mongodb","sql"}),
                (catName: "Web Development", title: "Xây dựng REST API chuẩn với .NET Core", content: "Chia sẻ best practices: 1) Repository + Unit of Work pattern, 2) DTOs và AutoMapper, 3) Validation với FluentValidation, 4) Error handling middleware, 5) Swagger documentation. Code example đầy đủ!", tags: new[]{"dotnet","csharp","restapi"}),
                (catName: "Cybersecurity", title: "Bảo mật ứng dụng web - Top 10 lỗ hổng OWASP", content: "OWASP Top 10 năm 2026: Injection, Broken Authentication, XSS, CSRF, Security Misconfiguration... Mình sẽ demo từng loại tấn công và cách phòng chống. Rất quan trọng cho mọi developer!", tags: new[]{"cybersecurity","linux"}),
                (catName: "Web Development", title: "Tailwind CSS - Tại sao mình bỏ Bootstrap để chuyển sang?", content: "Tailwind CSS cho phép custom UI nhanh gấp 3 lần so với viết CSS truyền thống. Utility-first approach, responsive design, dark mode dễ dàng. Mình sẽ share cách setup và best practices.", tags: new[]{"tailwindcss","bootstrap","react"}),
                (catName: "DevOps & Cloud", title: "CI/CD Pipeline với GitHub Actions", content: "Tự động hóa build, test, deploy với GitHub Actions. Mình chia sẻ workflow thực tế: lint → test → build Docker image → push to registry → deploy to AWS ECS. Tiết kiệm rất nhiều thời gian!", tags: new[]{"cicd","git","docker","aws"}),
                (catName: "Programming Languages", title: "Học TypeScript - Từ JavaScript lên level", content: "TypeScript giúp code JavaScript an toàn hơn, dễ maintain hơn. Chia sẻ: Type system, Generics, Utility types, Declaration files. Sau khi dùng TS, mình không muốn quay lại JS nữa!", tags: new[]{"typescript","javascript","nodejs"}),
                (catName: "Data Science", title: "Deep Learning với PyTorch - Hướng dẫn thực hành", content: "PyTorch dễ debug hơn TensorFlow nhờ dynamic computation graph. Bài viết hướng dẫn: Tensors, Autograd, Neural Networks, Training loop, Transfer Learning. Kèm notebook Colab miễn phí!", tags: new[]{"deeplearning","python","machinelearning"}),
                (catName: "Database", title: "Redis - Cache hiệu suất cao cho ứng dụng", content: "Redis không chỉ là cache, còn là message broker, session store, leaderboard. Mình chia sẻ cách tích hợp Redis vào .NET: caching patterns, pub/sub, distributed locking. Performance tăng 10x!", tags: new[]{"redis","sql","dotnet"}),
                (catName: "Web Development", title: "GraphQL vs REST API - Nên chọn gì?", content: "GraphQL cho phép client query chính xác data cần, giảm over-fetching. REST đơn giản, dễ cache, phổ biến. Mình đã dùng cả 2 trong production, chia sẻ ưu nhược điểm từng cái.", tags: new[]{"graphql","restapi","nodejs"}),
                (catName: "Mobile Development", title: "Publish app lên App Store và Google Play", content: "Kinh nghiệm submit app: 1) Chuẩn bị screenshots, description, 2) Review guidelines, 3) Code signing, 4) TestFlight/Internal testing, 5) Xử lý rejection. Nhiều bạn mới hay bị reject vì lỗi nhỏ!", tags: new[]{"swift","kotlin","flutter"}),
                (catName: "Web Development", title: "Microservices vs Monolith - Khi nào nên tách?", content: "Đừng microservices vì trend! Monolith phù hợp cho team nhỏ, dự án mới. Microservices cho hệ thống lớn, cần scale independent. Mình sẽ chia sẻ kinh nghiệm migrate monolith → microservices.", tags: new[]{"microservices","docker","restapi"}),
                (catName: "DevOps & Cloud", title: "AWS cho người mới - Bắt đầu từ đâu?", content: "AWS có 200+ services, rất dễ bị overwhelmed. Lộ trình mình recommend: EC2 → S3 → RDS → Lambda → API Gateway → CloudFront. Free tier đủ để học trong 12 tháng!", tags: new[]{"aws","linux","cicd"}),
                (catName: "Programming Languages", title: "Golang - Ngôn ngữ cho hệ thống hiệu suất cao", content: "Go đơn giản, compile nhanh, concurrency mạnh mẽ với goroutines. Phù hợp cho microservices, CLI tools, cloud infrastructure. Nhiều big tech đang chuyển sang Go. Ai muốn học cùng không?", tags: new[]{"golang","microservices","docker"}),
            };

            var random = new Random(42); // fixed seed for reproducible data
            for (int i = 0; i < postTemplates.Length; i++)
            {
                var tmpl = postTemplates[i];
                var author = postAuthors[i % postAuthors.Count];
                var cat = catLookup.ContainsKey(tmpl.catName) ? catLookup[tmpl.catName] : null;

                var post = new CommunityPost
                {
                    AuthorId = author.Id,
                    CategoryId = cat?.Id,
                    Title = tmpl.title,
                    Content = tmpl.content,
                    CreatedAt = now.AddDays(-random.Next(1, 30)).AddHours(-random.Next(1, 24)),
                    UpdatedAt = now.AddDays(-random.Next(0, 5))
                };
                await context.CommunityPosts.AddAsync(post);
                await context.SaveChangesAsync();

                // Link hashtags
                foreach (var tag in tmpl.tags)
                {
                    if (hashLookup.ContainsKey(tag))
                    {
                        await context.CommunityPostHashtags.AddAsync(new CommunityPostHashtag
                        {
                            PostId = post.Id,
                            HashtagId = hashLookup[tag].Id
                        });
                    }
                }

                // Add 1-3 comments
                var commentCount = random.Next(1, 4);
                for (int c = 0; c < commentCount; c++)
                {
                    var commenter = postAuthors[random.Next(postAuthors.Count)];
                    await context.Set<CommunityComment>().AddAsync(new CommunityComment
                    {
                        PostId = post.Id,
                        AuthorId = commenter.Id,
                        Content = GetRandomComment(random),
                        CreatedAt = post.CreatedAt?.AddHours(random.Next(1, 48))
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        private static string GetRandomComment(Random rng)
        {
            var comments = new[]
            {
                "Bài viết rất hữu ích, cảm ơn bạn đã chia sẻ!",
                "Mình đang cần tìm hiểu về vấn đề này, thanks!",
                "Có thể chia sẻ thêm tài liệu tham khảo được không?",
                "Hay quá! Mình sẽ áp dụng vào dự án của mình.",
                "Cảm ơn mentor, bài viết dễ hiểu lắm ạ.",
                "Mình có một câu hỏi, mentor có thể giải thích thêm phần này không?",
                "Đúng kinh nghiệm thực tế, mình cũng gặp vấn đề tương tự.",
                "🔥 Nội dung chất lượng!",
                "Follow mentor để đọc thêm bài viết mới nhé!",
                "Mình đã bookmark bài này rồi, rất bổ ích.",
                "Chia sẻ thêm về phần performance optimization được không ạ?",
                "Mình mới bắt đầu học, bài viết này giúp mình rất nhiều!",
            };
            return comments[rng.Next(comments.Length)];
        }
    }
}
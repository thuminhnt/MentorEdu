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
                    FullName = "Nguyen Van Student",
                    Phone = "0902000000",
                    Gender = "Male",
                    School = "Ho Chi Minh City University of Technology",
                    Major = "Computer Science",
                    Bio = "Third-year student, passionate about web and mobile programming. Looking for a mentor to guide my graduation project.",
                    City = "Ho Chi Minh",
                    Country = "Vietnam",
                    CreatedAt = now, UpdatedAt = now
                },
                new UserProfile
                {
                    UserId = mentor.Id,
                    FullName = "Tran Minh Mentor",
                    Phone = "0903000000",
                    Gender = "Male",
                    School = "University of Science",
                    Major = "Software Engineering",
                    Bio = "Senior Full-Stack Developer with 8 years of experience. Specializing in React, .NET, and Cloud Architecture.",
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
                Introduction = "Hi! I'm a full-stack developer with 8 years of experience at major tech companies. I specialize in React, .NET Core, and cloud computing. I can help you from basics to advanced topics.",
                AvailabilityNote = "Available for mentoring on weekday evenings and all day on weekends",
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
                ("mentor01@mentor-edu.local", "Le Thi Huong", "Female", "React & Frontend Expert", 150_000m, 1_200_000m, 5, "FPT University", "Frontend expert with 5 years of experience, especially in React and TypeScript.", new[]{"react","typescript","nextjs","tailwindcss"}, new[]{"Web Development"}),
                ("mentor02@mentor-edu.local", "Pham Quoc Bao", "Male", "Backend .NET Specialist", 180_000m, 1_400_000m, 6, "Ho Chi Minh City University of Technology", "Senior .NET developer, specializing in microservices and system design.", new[]{"dotnet","csharp","sql","restapi","microservices"}, new[]{"Web Development","Database"}),
                ("mentor03@mentor-edu.local", "Nguyen Hoai An", "Female", "Mobile Developer", 160_000m, 1_300_000m, 4, "University of Information Technology", "Mobile dev specializing in Flutter and React Native, published 10+ apps on store.", new[]{"flutter","reactnative","javascript","kotlin"}, new[]{"Mobile Development"}),
                ("mentor04@mentor-edu.local", "Truong Duc Huy", "Male", "Data Scientist", 250_000m, 2_000_000m, 7, "University of Science", "Data Scientist at VNG, specializing in ML/DL and NLP. Kaggle top 1% experience.", new[]{"python","machinelearning","deeplearning","datascience","sql"}, new[]{"Data Science"}),
                ("mentor05@mentor-edu.local", "Vo Thanh Tung", "Male", "DevOps Engineer", 220_000m, 1_800_000m, 6, "Da Nang University of Technology", "DevOps engineer specializing in AWS, Docker, Kubernetes. Helping you build CI/CD pipelines.", new[]{"docker","kubernetes","aws","cicd","linux","git"}, new[]{"DevOps & Cloud"}),
                ("mentor06@mentor-edu.local", "Dang Thi Mai", "Female", "UI/UX Designer", 140_000m, 1_000_000m, 4, "University of Fine Arts", "Lead designer at Tiki, specializing in user research and design system.", new[]{"figma","tailwindcss","bootstrap"}, new[]{"Design & UI/UX"}),
                ("mentor07@mentor-edu.local", "Bui Minh Duc", "Male", "Java Backend Developer", 190_000m, 1_500_000m, 7, "University of Engineering and Technology", "Senior Java developer at Shopee, specializing in Spring Boot and distributed systems.", new[]{"java","sql","postgresql","microservices","docker","restapi"}, new[]{"Programming Languages","Database"}),
                ("mentor08@mentor-edu.local", "Ly Ngoc Tram", "Female", "Python Full-Stack", 170_000m, 1_300_000m, 5, "FPT University", "Python developer specializing in Django/FastAPI and data engineering.", new[]{"python","javascript","postgresql","mongodb","restapi","graphql"}, new[]{"Web Development","Data Science"}),
                ("mentor09@mentor-edu.local", "Hoang Van Nam", "Male", "Cybersecurity Expert", 280_000m, 2_200_000m, 9, "University of Security", "Pentester & security consultant, OSCP certified.", new[]{"cybersecurity","linux","python"}, new[]{"Cybersecurity"}),
                ("mentor10@mentor-edu.local", "Phan Thi Yen Nhi", "Female", "Cloud Architect", 260_000m, 2_000_000m, 8, "Ho Chi Minh City University of Technology", "AWS Solutions Architect specializing in cloud-native system design.", new[]{"aws","azure","docker","kubernetes","cicd","microservices"}, new[]{"DevOps & Cloud","Web Development"}),
            };

            var mentorUsers = new List<User>();
            foreach (var (email, fullName, gender, title, hourly, package, exp, school, intro, tags, cats) in mentorData)
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
                    AvailabilityNote = "Available evenings and weekends",
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
                (catName: "Web Development", title: "React Guide from Basics to Advanced", content: "React is the most popular JavaScript library today. In this post I'll share a React learning roadmap from basics (JSX, Components, Props, State) to advanced (Hooks, Context, Redux, React Query). If you're learning React, let's discuss!", tags: new[]{"react","javascript","typescript"}),
                (catName: "Web Development", title: "Comparing Next.js vs Vite + React", content: "I've tried both stacks for a new project. Next.js is strong in SSR, SEO, built-in routing. Vite + React is faster, lighter, more flexible. Choose based on your project. What stack do you use?", tags: new[]{"nextjs","react","javascript"}),
                (catName: "Programming Languages", title: "Why You Should Learn C# and .NET in 2026", content: ".NET 9 has launched with amazing performance improvements. C# is increasingly modern with pattern matching, record types, and AOT compilation. The .NET ecosystem is strong for enterprise, gaming (Unity), and cloud. If you're considering learning it, now is a great time!", tags: new[]{"csharp","dotnet","restapi"}),
                (catName: "Mobile Development", title: "Flutter vs React Native 2026 - Which One to Choose?", content: "I've used both frameworks for real projects. Flutter is strong in UI consistency and performance. React Native is strong in ecosystem and code sharing with web. Sharing my hands-on experience.", tags: new[]{"flutter","reactnative","javascript"}),
                (catName: "Data Science", title: "Machine Learning Roadmap for Beginners", content: "Step 1: Learn basic Python -> Step 2: Numpy, Pandas -> Step 3: Scikit-learn -> Step 4: Deep Learning (TensorFlow/PyTorch) -> Step 5: Build real projects. I'll recommend specific resources for each step.", tags: new[]{"python","machinelearning","datascience"}),
                (catName: "DevOps & Cloud", title: "Docker for Beginners", content: "Docker helps package applications into containers that can run anywhere. This post covers: Dockerfile, docker-compose, networking, volumes. At the end there's a demo containerizing a .NET + PostgreSQL application.", tags: new[]{"docker","kubernetes","linux"}),
                (catName: "Design & UI/UX", title: "Effective UI/UX Design with Figma", content: "Figma is the most loved design tool today. My tips: 1) Use Auto Layout, 2) Create a Design System, 3) Component variants, 4) Prototyping. Need a Figma template? I'll share!", tags: new[]{"figma","tailwindcss"}),
                (catName: "Database", title: "PostgreSQL vs MongoDB - When to Use Which?", content: "PostgreSQL for structured data, ACID transactions, complex queries. MongoDB for flexible data, horizontal scaling, real-time analytics. I'll analyze each specific use case.", tags: new[]{"postgresql","mongodb","sql"}),
                (catName: "Web Development", title: "Building a Proper REST API with .NET Core", content: "Sharing best practices: 1) Repository + Unit of Work pattern, 2) DTOs and AutoMapper, 3) Validation with FluentValidation, 4) Error handling middleware, 5) Swagger documentation. Full code examples!", tags: new[]{"dotnet","csharp","restapi"}),
                (catName: "Cybersecurity", title: "Web Application Security - OWASP Top 10 Vulnerabilities", content: "OWASP Top 10 in 2026: Injection, Broken Authentication, XSS, CSRF, Security Misconfiguration... I'll demo each type of attack and how to prevent them. Very important for every developer!", tags: new[]{"cybersecurity","linux"}),
                (catName: "Web Development", title: "Tailwind CSS - Why I Switched from Bootstrap", content: "Tailwind CSS lets you customize UI 3x faster than writing traditional CSS. Utility-first approach, responsive design, easy dark mode. I'll share setup and best practices.", tags: new[]{"tailwindcss","bootstrap","react"}),
                (catName: "DevOps & Cloud", title: "CI/CD Pipeline with GitHub Actions", content: "Automate build, test, deploy with GitHub Actions. Sharing a real workflow: lint -> test -> build Docker image -> push to registry -> deploy to AWS ECS. Saves so much time!", tags: new[]{"cicd","git","docker","aws"}),
                (catName: "Programming Languages", title: "Learning TypeScript - Leveling Up from JavaScript", content: "TypeScript makes JavaScript code safer and easier to maintain. Topics: Type system, Generics, Utility types, Declaration files. After using TS, I never want to go back to JS!", tags: new[]{"typescript","javascript","nodejs"}),
                (catName: "Data Science", title: "Deep Learning with PyTorch - Practical Guide", content: "PyTorch is easier to debug than TensorFlow thanks to dynamic computation graphs. This post covers: Tensors, Autograd, Neural Networks, Training loop, Transfer Learning. Includes free Colab notebook!", tags: new[]{"deeplearning","python","machinelearning"}),
                (catName: "Database", title: "Redis - High Performance Cache for Applications", content: "Redis is not just a cache, it's also a message broker, session store, leaderboard. I'll share how to integrate Redis with .NET: caching patterns, pub/sub, distributed locking. 10x performance boost!", tags: new[]{"redis","sql","dotnet"}),
                (catName: "Web Development", title: "GraphQL vs REST API - Which to Choose?", content: "GraphQL allows clients to query exactly the data needed, reducing over-fetching. REST is simpler, easier to cache, more widespread. I've used both in production, sharing pros and cons of each.", tags: new[]{"graphql","restapi","nodejs"}),
                (catName: "Mobile Development", title: "Publishing Apps on App Store and Google Play", content: "App submission experience: 1) Prepare screenshots, description, 2) Review guidelines, 3) Code signing, 4) TestFlight/Internal testing, 5) Handling rejection. Many newcomers get rejected for small mistakes!", tags: new[]{"swift","kotlin","flutter"}),
                (catName: "Web Development", title: "Microservices vs Monolith - When to Split?", content: "Don't go microservices just for the trend! Monolith suits small teams and new projects. Microservices for large systems needing independent scaling. I'll share my experience migrating from monolith to microservices.", tags: new[]{"microservices","docker","restapi"}),
                (catName: "DevOps & Cloud", title: "AWS for Beginners - Where to Start?", content: "AWS has 200+ services, very easy to get overwhelmed. My recommended roadmap: EC2 -> S3 -> RDS -> Lambda -> API Gateway -> CloudFront. Free tier is enough to learn for 12 months!", tags: new[]{"aws","linux","cicd"}),
                (catName: "Programming Languages", title: "Golang - Language for High-Performance Systems", content: "Go is simple, compiles fast, powerful concurrency with goroutines. Suitable for microservices, CLI tools, cloud infrastructure. Many big tech companies are switching to Go. Want to learn together?", tags: new[]{"golang","microservices","docker"}),
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
                "Very helpful post, thanks for sharing!",
                "I was looking into this topic, thanks!",
                "Could you share more reference materials?",
                "Great! I'll apply this to my project.",
                "Thank you mentor, the post is very easy to understand.",
                "I have a question, could you explain this part more?",
                "Real-world experience, I've encountered the same issue.",
                "🔥 Quality content!",
                "Follow the mentor for more new posts!",
                "I've bookmarked this post, very informative.",
                "Could you share more about performance optimization?",
                "I just started learning, this post helped me a lot!",
            };
            return comments[rng.Next(comments.Length)];
        }
    }
}
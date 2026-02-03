using MentorEdu.Data;
using MentorEdu.Hubs;
using MentorEdu.Models;
using MentorEdu.Repositories.Implementations;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Implementations;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =======================
// DATABASE
// =======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// =======================
// IDENTITY
// =======================
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// JWT CONFIG
// =======================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "MentorEduSecretKey123456789012345678901234567890";

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MentorEdu";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MentorEduApp";

// =======================
// AUTHENTICATION
// =======================
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // SignalR JWT Authentication
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// =======================
// GOOGLE OAUTH (OPTIONAL)
// =======================
if (!string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var uri = context.RedirectUri + "&prompt=select_account";
            context.Response.Redirect(uri);
            return Task.CompletedTask;
        };

        // Handle when user cancels or denies Google login
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/api/Auth/google-callback?error=access_denied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

// =======================
// SIGNALR
// =======================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});

// =======================
// CORS
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

// =======================
// REPOSITORIES
// =======================
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IMentorProfileRepository, MentorProfileRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IHashtagRepository, HashtagRepository>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IProposalRepository, ProposalRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
builder.Services.AddScoped<IWorkSessionRepository, WorkSessionRepository>();
builder.Services.AddScoped<ISessionTrackingRepository, SessionTrackingRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IEscrowRepository, EscrowRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ICommunityPostRepository, CommunityPostRepository>();
builder.Services.AddScoped<ICommunityCommentRepository, CommunityCommentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
builder.Services.AddScoped<IWorkSessionService, WorkSessionService>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
builder.Services.AddScoped<IGroupMessageRepository, GroupMessageRepository>();
// =======================
// UNIT OF WORK
// =======================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// =======================
// EMAIL SETTINGS
// =======================
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// =======================
// SERVICES
// =======================
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMentorService, MentorService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IProposalService, ProposalService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IGroupService, GroupService>();

// =======================
// APP
// =======================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MentorEdu API V1");
        c.RoutePrefix = string.Empty; // serve Swagger UI at root '/'
    });

    // Redirect /index.html to root to handle browsers that request index.html explicitly
    app.MapGet("/index.html", () => Results.Redirect("/"))
        .ExcludeFromDescription();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// CORS phải đặt TRƯỚC Auth
app.UseCors("AllowReactApp");
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// API Controllers
app.MapControllers();

// MVC Controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// SignalR Hub
app.MapHub<ChatHub>("/hubs/chat");

// =======================
// SEED DATABASE
// =======================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Ensure database is created and all migrations are applied
    await context.Database.MigrateAsync();
    
    await DbSeeder.SeedAsync(context);
}

app.Run();

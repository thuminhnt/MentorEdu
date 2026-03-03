using MentorEdu.Data;
using MentorEdu.Hubs;
using MentorEdu.Middleware;
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
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
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
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
});
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
        //.WithOrigins("http://localhost:5173")
            .SetIsOriginAllowed(_ => true) // Allow all origins (dev)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

// =======================
// REPOSITORIES
// =======================
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
// Auto-register tất cả Repositories + UnitOfWork qua assembly scan (Scrutor)
builder.Services.Scan(scan => scan
    .FromAssemblyOf<UserRepository>()
    .AddClasses(c => c.InNamespaces("MentorEdu.Repositories.Implementations")
        .Where(t => !t.IsGenericTypeDefinition))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

// =======================
// MOMO SETTINGS
// =======================
builder.Services.Configure<MentorEdu.Models.MoMoSettings>(builder.Configuration.GetSection("MoMo"));

// =======================
// VNPAY SETTINGS
// =======================
builder.Services.Configure<MentorEdu.Models.VNPaySettings>(builder.Configuration.GetSection("VNPay"));

// =======================
// VIETQR SETTINGS
// =======================
builder.Services.Configure<MentorEdu.Models.VietQRSettings>(builder.Configuration.GetSection("VietQR"));

// =======================
// EMAIL SETTINGS
// =======================
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// =======================
// SERVICES
// =======================
builder.Services.AddHttpClient();
// Auto-register tất cả Services qua assembly scan (Scrutor)
builder.Services.Scan(scan => scan
    .FromAssemblyOf<AuthService>()
    .AddClasses(c => c.InNamespaces("MentorEdu.Services.Implementations")
        .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition && !typeof(Exception).IsAssignableFrom(t)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
builder.Services.AddHttpContextAccessor();

// =======================
// APP
// =======================
var app = builder.Build();

// Global Exception Handler — đặt đầu pipeline để bắt tất cả exception
app.UseGlobalExceptionHandler();

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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// CORS phải đặt TRƯỚC Auth
app.UseCors("AllowReactApp");
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

    // Recalculate mentor ratings from Reviews table (fix stale/zero ratingAvg)
    var mentorProfiles = await context.MentorProfiles.ToListAsync();
    foreach (var mp in mentorProfiles)
    {
        var reviewStats = await context.Reviews
            .Where(r => r.ToUserId == mp.UserId)
            .GroupBy(r => r.ToUserId)
            .Select(g => new { Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync();

        if (reviewStats != null)
        {
            mp.RatingAvg = (decimal)reviewStats.Avg;
            mp.RatingCount = reviewStats.Count;
        }
    }
    await context.SaveChangesAsync();
}

app.Run();

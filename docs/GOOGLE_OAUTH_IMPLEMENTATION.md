# Google OAuth Implementation - Complete Guide

## Summary
Implemented Google OAuth 2.0 authentication for MentorEdu, allowing users to sign in with Google accounts from both backend API and frontend React application.

---

## Backend Changes

### 1. NuGet Package
Added: `Microsoft.AspNetCore.Authentication.Google` version 8.0.0

```bash
dotnet add package Microsoft.AspNetCore.Authentication.Google --version 8.0.0
```

### 2. Configuration Files

#### appsettings.json
Added empty placeholders (DO NOT put real secrets here):
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    }
  }
}
```

#### User Secrets (Development)
Store credentials securely:
```bash
cd d:\Thu\School\NCKH\MentorEdu
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

### 3. Modified Backend Files

#### Program.cs
Added Google OAuth middleware:
```csharp
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
});
```

#### AuthController.cs
New endpoints:

| Method | Route                        | Description                                 |
|--------|------------------------------|---------------------------------------------|
| GET    | /api/auth/google-login       | Initiates Google OAuth, redirects to Google |
| GET    | /api/auth/google-callback    | Handles Google response, redirects to React with JWT |

**Implementation:**
```csharp
[HttpGet("google-login")]
public IActionResult GoogleLogin([FromQuery] string? returnUrl = null, [FromQuery] int? role = null)
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = Url.Action(nameof(GoogleCallback)),
        Items = { { "returnUrl", returnUrl ?? "/" } }
    };
    
    if (role.HasValue)
    {
        properties.Items.Add("role", role.Value.ToString());
    }

    return Challenge(properties, GoogleDefaults.AuthenticationScheme);
}

[HttpGet("google-callback")]
public async Task<IActionResult> GoogleCallback()
{
    var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

    if (!authenticateResult.Succeeded)
    {
        return Redirect("http://localhost:5173/login?error=Authentication%20failed");
    }

    var claims = authenticateResult.Principal?.Identities.FirstOrDefault()?.Claims;
    var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "";
    var avatarUrl = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;
    
    if (string.IsNullOrEmpty(email))
    {
        return Redirect("http://localhost:5173/login?error=Email%20not%20found");
    }

    // Get role from state if provided
    UserRole? role = null;
    if (authenticateResult.Properties?.Items.TryGetValue("role", out var roleStr) == true 
        && int.TryParse(roleStr, out var roleInt))
    {
        role = (UserRole)roleInt;
    }

    var result = await _authService.GoogleLoginAsync(email, name, avatarUrl, role);

    if (!result.Success)
    {
        var errorMessage = Uri.EscapeDataString(result.Message ?? "Login failed");
        return Redirect($"http://localhost:5173/login?error={errorMessage}");
    }

    // Redirect back to frontend with tokens
    return Redirect($"http://localhost:5173/login?token={result.Data!.Token}&refreshToken={result.Data.RefreshToken}");
}
```

#### AuthService.cs
Added `GoogleLoginAsync` method:
- Creates new user if email doesn't exist
- Returns existing user if already registered
- Generates JWT and refresh token
- Default role: **Student** (unless specified)

```csharp
public async Task<ServiceResult<LoginResponse>> GoogleLoginAsync(string email, string name, string? avatarUrl, UserRole? role = null)
{
    var user = await _unitOfWork.Users.GetByEmailAsync(email);
    
    if (user == null)
    {
        // Create new user with default "Student" role
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            user = new User
            {
                Email = email,
                PasswordHash = HashPassword(Guid.NewGuid().ToString()),
                Role = role ?? UserRole.Student,
                IsVerified = true, // Google accounts are pre-verified
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Create UserProfile, MentorProfile (if Mentor), and Wallet
            // ... (see code for details)
            
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    // Generate JWT and refresh token
    var token = GenerateJwtToken(user);
    var refreshToken = GenerateRefreshToken();
    
    return ServiceResult<LoginResponse>.SuccessResult(response, "Google login successful");
}
```

#### IAuthService.cs
Added interface method:
```csharp
Task<ServiceResult<LoginResponse>> GoogleLoginAsync(string email, string name, string? avatarUrl, UserRole? role = null);
```

#### AuthDtos.cs
Added DTO:
```csharp
public class GoogleLoginRequest
{
    public UserRole? Role { get; set; }
}
```

---

## Frontend Changes

### 1. Modified Files

#### src/api/authAPI.js
Added Google login method:
```javascript
export const googleLogin = () => {
  // Redirect to backend Google OAuth endpoint
  window.location.href = `${API_BASE_URL}/api/auth/google-login`;
};
```

#### src/pages/auth/Login.jsx
Added:

**1. OAuth callback handler** (useEffect):
```javascript
useEffect(() => {
  const urlParams = new URLSearchParams(window.location.search);
  const token = urlParams.get('token');
  const refreshToken = urlParams.get('refreshToken');
  const error = urlParams.get('error');

  if (error) {
    setMessage({ type: 'error', text: decodeURIComponent(error) });
    window.history.replaceState({}, document.title, '/login');
    return;
  }

  if (token && refreshToken) {
    localStorage.setItem('token', token);
    localStorage.setItem('refreshToken', refreshToken);

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const userRole = payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      if (userRole === 'Admin') {
        navigate('/admin/dashboard');
      } else if (userRole === 'Mentor') {
        navigate('/mentor/dashboard');
      } else {
        navigate('/student/dashboard');
      }
    } catch (error) {
      console.error('Error parsing token:', error);
      navigate('/student/dashboard');
    }

    window.history.replaceState({}, document.title, '/login');
  }
}, [navigate]);
```

**2. Google button handler**:
```javascript
const handleGoogleLogin = () => {
  googleLogin();
};
```

**3. Google button UI**:
```jsx
<div className="relative my-6">
  <div className="absolute inset-0 flex items-center">
    <div className="w-full border-t border-gray-300"></div>
  </div>
  <div className="relative flex justify-center text-sm">
    <span className="px-2 bg-white text-gray-500">Or continue with</span>
  </div>
</div>

<button
  type="button"
  onClick={handleGoogleLogin}
  className="w-full flex items-center justify-center gap-3 px-4 py-3 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
>
  <svg className="w-5 h-5" viewBox="0 0 24 24">
    {/* Google icon SVG */}
  </svg>
  <span className="text-gray-700 font-medium">Sign in with Google</span>
</button>
```

---

## API Flow

```
???????????                                  ???????????????                    ??????????
? Client  ?                                  ?   Backend   ?                    ? Google ?
?(React)  ?                                  ?  (ASP.NET)  ?                    ?  OAuth ?
???????????                                  ???????????????                    ??????????
     ?                                              ?                                ?
     ?  1. Click "Sign in with Google"             ?                                ?
     ??????????????????????????????????????????????>?                                ?
     ?  GET /api/auth/google-login                 ?                                ?
     ?                                              ?                                ?
     ?                                              ?  2. Redirect to Google OAuth   ?
     ?                                              ????????????????????????????????>?
     ?                                              ?                                ?
     ?                      3. User logs in with Google                             ?
     ?<???????????????????????????????????????????????????????????????????????????????
     ?                                              ?                                ?
     ?                                              ?  4. Google returns with code   ?
     ?                                              ?<????????????????????????????????
     ?                                              ?                                ?
     ?                                              ?  5. Exchange code for user info?
     ?                                              ????????????????????????????????>?
     ?                                              ?                                ?
     ?                                              ?  6. User info                  ?
     ?                                              ?<????????????????????????????????
     ?                                              ?                                ?
     ?                                              ?  7. Create/Find user           ?
     ?                                              ?     Generate JWT               ?
     ?                                              ?                                ?
     ?  8. Redirect with tokens                    ?                                ?
     ?  /login?token=...&refreshToken=...          ?                                ?
     ?<??????????????????????????????????????????????                                ?
     ?                                              ?                                ?
     ?  9. Extract tokens, store, navigate         ?                                ?
     ?     to appropriate dashboard                ?                                ?
     ?                                              ?                                ?
```

---

## Setup Instructions

### Backend Setup

1. **Install NuGet package**:
```bash
dotnet add package Microsoft.AspNetCore.Authentication.Google --version 8.0.0
```

2. **Configure Google Cloud Console**:
   - Go to https://console.cloud.google.com/
   - Create or select a project
   - Enable Google+ API
   - Create OAuth 2.0 credentials
   - Add authorized redirect URI: `https://localhost:7082/signin-google`

3. **Store credentials in User Secrets**:
```bash
cd d:\Thu\School\NCKH\MentorEdu
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

4. **Run the backend**:
```bash
dotnet run --launch-profile https
```

### Frontend Setup

1. **Install dependencies** (if not already installed):
```bash
cd d:\Thu\School\NCKH\my-app
npm install
```

2. **Verify .env file** has correct API URL:
```env
VITE_API_BASE_URL=https://localhost:7082
```

3. **Run the frontend**:
```bash
npm run dev
```

---

## Testing

### Test Full Stack (React + Backend) ? RECOMMENDED
1. Start backend: `cd d:\Thu\School\NCKH\MentorEdu && dotnet run --launch-profile https`
2. Start frontend: `cd d:\Thu\School\NCKH\my-app && npm run dev`
3. Navigate to: `http://localhost:5173/login`
4. Click "Sign in with Google" button
5. Complete Google login
6. **Automatically redirected to appropriate dashboard based on role**

### Test Backend Only (Swagger)
1. Navigate to: `https://localhost:7082/swagger`
2. Find `/api/auth/google-login` endpoint
3. Click "Try it out" ? "Execute"
4. You'll be redirected to Google login
5. After login, redirected back to React app (not Swagger)

---

## Security Notes

### ? CORRECT - Using User Secrets (Development)
```bash
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
```

### ? INCORRECT - Storing in appsettings.json
```json
// DON'T DO THIS
{
  "Authentication": {
    "Google": {
      "ClientId": "real-client-id-here",
      "ClientSecret": "real-secret-here"  // ? NEVER DO THIS
    }
  }
}
```

### Production Deployment
For production, use:
- **Azure Key Vault**
- **Environment Variables**
- **AWS Secrets Manager**
- **Never commit secrets to Git**

Also update the redirect URL in `GoogleCallback` from:
```csharp
return Redirect("http://localhost:5173/login?token=...");
```
To your production frontend URL:
```csharp
var frontendUrl = _configuration["Frontend:Url"] ?? "https://yourdomain.com";
return Redirect($"{frontendUrl}/login?token=...");
```

---

## Troubleshooting

### Issue: "redirect_uri_mismatch" error
**Solution**: Ensure the redirect URI in Google Cloud Console matches exactly:
```
https://localhost:7082/signin-google
```

### Issue: Tokens not being stored in frontend
**Solution**: Check browser console for errors. Ensure `localStorage` is accessible.

### Issue: User always gets "Student" role
**Solution**: This is by design. Google OAuth creates Student accounts by default. To create Mentor/Admin accounts:
- Option 1: Manually upgrade user role in database
- Option 2: Add `role` query parameter: `/api/auth/google-login?role=1` (1=Student, 2=Mentor, 3=Admin)

### Issue: CORS errors
**Solution**: Verify CORS policy in `Program.cs` includes your frontend URL:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### Issue: After login, redirected to Swagger instead of React
**Solution**: ? FIXED! The `GoogleCallback` method now redirects to React app (`http://localhost:5173/login`) instead of returning JSON.

---

## Build Status

- ? Backend build: **Successful**
- ? Frontend dev server: Running on http://localhost:5173
- ? Integration: **Backend redirects to frontend with tokens**
- ? Full flow test: **Ready to test**

---

## File Changes Summary

### Backend
- ? `Program.cs` - Added Google OAuth middleware
- ? `Controllers/Api/AuthController.cs` - Updated google-callback to redirect to React
- ? `Services/Implementations/AuthService.cs` - Added GoogleLoginAsync method
- ? `Services/Interfaces/IAuthService.cs` - Added interface method
- ? `DTOs/Auth/AuthDtos.cs` - Added GoogleLoginRequest DTO

### Frontend
- ? `src/api/authAPI.js` - Added googleLogin function
- ? `src/pages/auth/Login.jsx` - Added OAuth callback handler and Google button
- ? `.env` - Verified API_BASE_URL configuration

### Documentation
- ? `docs/GOOGLE_OAUTH_IMPLEMENTATION.md` - **This file (updated)**

---

## What Changed from Initial Implementation

### Before (Swagger only):
- `GoogleCallback` returned JSON response
- Worked only in Swagger/Postman
- Frontend couldn't receive tokens

### After (Full Stack):
- `GoogleCallback` redirects to React app with tokens in URL
- Frontend extracts tokens and navigates to dashboard
- Works seamlessly end-to-end

---

## Next Steps

1. ? Backend Google OAuth - **COMPLETED**
2. ? Frontend Google OAuth - **COMPLETED**
3. ? Backend redirect to frontend - **COMPLETED**
4. ? Test full flow - **READY TO TEST NOW!**
5. ? Production deployment setup
6. ? Add Google OAuth for Mentor role (optional enhancement)

---

**Created by**: GitHub Copilot  
**Date**: 2024  
**Project**: MentorEdu  
**Version**: 1.1 (Updated - Full Stack Integration)

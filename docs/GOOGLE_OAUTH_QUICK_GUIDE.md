# Google OAuth - Quick Reference Guide

## ? What Was Done

### Backend (ASP.NET Core)
1. Added `Microsoft.AspNetCore.Authentication.Google` package
2. Updated `Program.cs` with Google OAuth middleware
3. **Fixed `AuthController.cs` GoogleCallback** - Now redirects to React app with tokens:
   ```csharp
   return Redirect($"http://localhost:5173/login?token={token}&refreshToken={refreshToken}");
   ```
4. `AuthService.cs` creates users with default "Student" role

### Frontend (React)
1. Added `googleLogin()` in `authAPI.js`
2. Added Google button in `Login.jsx`
3. Added `useEffect` to handle OAuth callback and extract tokens

---

## ?? How to Test

### Start Backend
```bash
cd d:\Thu\School\NCKH\MentorEdu
dotnet run --launch-profile https
```
Backend runs on: `https://localhost:7082`

### Start Frontend
```bash
cd d:\Thu\School\NCKH\my-app
npm run dev
```
Frontend runs on: `http://localhost:5173`

### Test Login Flow
1. Open: `http://localhost:5173/login`
2. Click **"Sign in with Google"** button
3. Login with Google account
4. **Automatically redirected to dashboard** (Student/Mentor/Admin based on role)

---

## ?? Configuration

### User Secrets (Already Set)
```bash
cd d:\Thu\School\NCKH\MentorEdu
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

### Google Cloud Console
- Authorized redirect URI: `https://localhost:7082/signin-google`

---

## ?? Important Notes

### Default Role
- All Google OAuth users are created as **Student** role by default
- To create Mentor/Admin, manually update database or use role parameter

### Redirect Flow
```
User ? Click Google button 
  ? Backend (/api/auth/google-login) 
  ? Google login 
  ? Backend callback (/api/auth/google-callback) 
  ? Redirect to React (http://localhost:5173/login?token=...&refreshToken=...)
  ? Frontend extracts tokens 
  ? Navigate to dashboard
```

### Security
- ? ClientSecret stored in User Secrets (not in appsettings.json)
- ? Tokens passed via URL (then stored in localStorage)
- ? CORS configured for React app

---

## ?? Troubleshooting

### "redirect_uri_mismatch"
Check Google Cloud Console redirect URI: `https://localhost:7082/signin-google`

### "Tokens not stored"
Check browser console for JavaScript errors

### "Always redirects to Swagger"
? **FIXED!** Backend now redirects to `http://localhost:5173/login`

---

## ?? Full Documentation
See: `docs/GOOGLE_OAUTH_IMPLEMENTATION.md`

---

**Status**: ? **READY TO TEST**  
**Last Updated**: 2024  
**Project**: MentorEdu

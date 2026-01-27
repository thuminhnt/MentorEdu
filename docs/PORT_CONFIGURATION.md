# Port Configuration Guide

## Production Ports

This application is configured to use **ONLY TWO PORTS**:

- **Frontend (Vite)**: `http://localhost:5173`
- **Backend (.NET)**: `https://localhost:7082`

## Important Notes

⚠️ **DO NOT open additional ports unnecessarily**

The backend is configured to run **ONLY on HTTPS port 7082**. The HTTP port 5185 has been intentionally removed to avoid confusion and unnecessary port usage.

## Configuration Files

### Backend Port Configuration
File: `Properties/launchSettings.json`

The default profile uses:
```json
{
  "applicationUrl": "https://localhost:7082"
}
```

### Frontend API Configuration
File: `.env` (in my-app folder)

```env
VITE_API_BASE_URL=https://localhost:7082
```

## How to Run

### Start Backend
```bash
cd MentorEdu
dotnet run
```
Backend will start on: `https://localhost:7082`

### Start Frontend
```bash
cd my-app
npm run dev
```
Frontend will start on: `http://localhost:5173`

## Troubleshooting

If you see errors about port 5185 or other ports:
1. Check `Properties/launchSettings.json` - should only have port 7082
2. Check frontend `.env` - should point to `https://localhost:7082`
3. Kill any lingering processes:
   ```powershell
   Get-Process | Where-Object {$_.ProcessName -like '*dotnet*'} | Stop-Process -Force
   Get-Process | Where-Object {$_.ProcessName -like '*node*'} | Stop-Process -Force
   ```

## Why Only These Two Ports?

- **5173**: Standard Vite development server port
- **7082**: Single HTTPS endpoint for backend API (secure, no HTTP fallback needed)

This minimal configuration:
- ✅ Reduces confusion
- ✅ Easier to debug
- ✅ Better security (HTTPS only)
- ✅ Cleaner development environment

---
**Last Updated**: January 27, 2026

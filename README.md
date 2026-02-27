# EduMentor Backend

ASP.NET Core 8 Web API — Nền tảng kết nối Mentor-Student.

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB hoặc full)
- Node.js 18+ (cho frontend `my-app`)
- ngrok (cho MoMo/VNPay IPN callback)

---

## 1. Clone & Restore

```bash
git clone <repo-url>
cd MentorEdu
dotnet restore
```

## 2. Cấu hình User Secrets (API Keys)

Các API key **không lưu trong `appsettings.json`** mà dùng `dotnet user-secrets` để bảo mật.

Chạy lần lượt các lệnh sau trong folder `MentorEdu/`:

```bash
# Email (SendGrid)
dotnet user-secrets set "EmailSettings:SendGridApiKey" "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"
dotnet user-secrets set "EmailSettings:SenderEmail" "nguyentranminhthu233@gmail.com"
dotnet user-secrets set "EmailSettings:SenderName" "EduMentor"

# Google OAuth
dotnet user-secrets set "Authentication:Google:ClientId" "94658155587-f3l3m1o4pp7pa35n6c0nlfhsmhao3f6e.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "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"

# Gemini AI
dotnet user-secrets set "GeminiAI:ApiKey" "AIzaSyAZkshDVoMsTnadt0ZS_Kax2dTiEdzDp8o"
dotnet user-secrets set "GeminiAI:ChatApiKey" "AIzaSyC8prr9wxL4Lp4bb4A0Y_fQrbLGIEsyX5U"

# MoMo Sandbox
dotnet user-secrets set "MoMo:AccessKey" "klm05TvNBzhg7h7j"
dotnet user-secrets set "MoMo:SecretKey" "at67qH6mk8w5Y1nAyMoYKMWACiEi2bsa"

# VNPay Sandbox
dotnet user-secrets set "VNPay:TmnCode" "UHRQ3XGS"
dotnet user-secrets set "VNPay:HashSecret" "YK8CXPM7WQH8E0ABVSC8KRE4EBK3SXMO"

# JWT
dotnet user-secrets set "Jwt:Key" "MentorEduSecretKey123456789012345678901234567890"
```

> Kiểm tra lại: `dotnet user-secrets list`

## 3. Cấu hình ngrok (bắt buộc cho thanh toán MoMo/VNPay)

MoMo và VNPay gửi IPN callback (server-to-server) nên cần URL public. Dùng ngrok để tạo tunnel.

**Bước 1**: Login trên [ngrok.com](https://ngrok.com), bấm vào **Authtokens** → chọn **Add Authtoken** → copy cột ID, chạy:

```bash
ngrok config add-authtoken 2xxxxxxxxxxxxxxxxxxxxx
```

Sau đó khởi chạy tunnel:

```bash
ngrok http https://localhost:7082
```

> Dùng `https://localhost:7082` thay vì chỉ số port vì backend chạy HTTPS.

Sau khi chạy, ngrok sẽ hiện URL kiểu:

```
Forwarding   https://abc123def456.ngrok-free.app -> https://localhost:7082
```

**Bước 2**: Copy URL ngrok và cập nhật `appsettings.json`:

```json
"MoMo": {
    "IpnUrl": "https://abc123def456.ngrok-free.app/api/Wallet/momo/ipn"
},
"VNPay": {
    "IpnUrl": "https://abc123def456.ngrok-free.app/api/Wallet/vnpay/ipn"
}
```

> ⚠️ **Lưu ý**: Mỗi lần khởi động lại ngrok, URL sẽ đổi → phải cập nhật lại `IpnUrl` trong `appsettings.json`.

## 4. Chạy ứng dụng

**Bước 3**: Mở 2 terminal:

```bash
# Terminal 1 — Backend (folder MentorEdu)
dotnet run

# Terminal 2 — Frontend (folder my-app)
cd my-app
npm install
npm run dev
```

Truy cập: **http://localhost:5173**

Backend API: **https://localhost:7082**

Swagger: **https://localhost:7082/swagger**

## 5. Thanh toán Test

### MoMo Sandbox
Login → Wallet → Deposit → chọn MoMo → nhập số tiền → xác nhận → redirect sang trang MoMo test.

### VNPay Sandbox
Login → Wallet → Deposit → chọn VNPay → nhập số tiền → xác nhận → redirect sang VNPay.

Thẻ test wallet: https://sandbox.vnpayment.vn/apis/vnpay-demo/

Thẻ test:
| Ngân hàng | Số thẻ | Tên chủ thẻ | Ngày phát hành | OTP |
|-----------|--------|-------------|----------------|-----|
| NCB | 9704198526191432198 | NGUYEN VAN A | 07/15 | 123456 |

### Bank Transfer (VietQR)
Login → Wallet → Deposit → chọn Bank Transfer → nhập số tiền → quét QR → chuyển khoản → Admin xác nhận.

## 6. Database

```bash
# Tạo migration mới
dotnet ef migrations add MigrationName

# Áp dụng migration
dotnet ef database update
```

Database tự tạo khi chạy `dotnet run` lần đầu (nếu chưa có).

## Project Structure

```
MentorEdu/
├── Controllers/Api/    # API Controllers
├── Services/           # Business Logic Layer
├── Repositories/       # Data Access Layer
├── Models/             # Domain Models & Enums
├── DTOs/               # Data Transfer Objects
├── Data/               # DbContext & Migrations
├── Hubs/               # SignalR (Chat)
└── docs/               # Documentation

my-app/                 # React + Vite Frontend
├── src/
│   ├── api/            # Axios API clients
│   ├── components/     # Reusable components
│   ├── pages/          # Page components
│   └── routes/         # App routing
└── public/
```

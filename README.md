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

Chạy lần lượt các lệnh trong file key-secrets vào trong folder `MentorEdu/`.

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

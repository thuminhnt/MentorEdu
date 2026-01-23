# Admin API Documentation

## T?ng quan

Tài li?u này mô t? các API endpoints m?i ???c thêm vào h? th?ng ?? Admin có th? qu?n lý toàn b? d? li?u c?a ?ng d?ng MentorEdu.

**Base URL**: `/api/admin`

**Authentication**: T?t c? endpoints yêu c?u JWT token v?i role `Admin`

**Header yêu c?u**:
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

---

## ?? Dashboard

### GET `/api/admin/dashboard`

**Mô t?**: L?y th?ng kê t?ng quan cho admin dashboard

**Response**:
```json
{
  "success": true,
  "data": {
    "totalUsers": 150,
    "totalStudents": 100,
    "totalMentors": 45,
    "totalOrders": 85,
    "activeOrders": 12,
    "completedOrders": 60,
    "disputedOrders": 3,
    "pendingReports": 5,
    "totalRequests": 120,
    "totalProposals": 95,
    "totalPosts": 200,
    "totalRevenue": 50000000,
    "totalEscrowHolding": 5000000,
    "recentActivities": [
      {
        "type": "NewOrder",
        "description": "New order created by Student A",
        "createdAt": "2026-01-23T10:30:00Z"
      }
    ]
  }
}
```

---

## ?? User Management

### GET `/api/admin/users`

**Mô t?**: L?y danh sách users v?i filter và phân trang

**Query Parameters**:
- `pageNumber` (int, default: 1) - S? trang
- `pageSize` (int, default: 10) - S? items m?i trang
- `role` (UserRole enum, optional) - Filter theo role (Student, Mentor, Admin)
- `isActive` (bool, optional) - Filter theo tr?ng thái active

**Example**: `/api/admin/users?pageNumber=1&pageSize=20&role=Mentor&isActive=true`

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "email": "mentor@example.com",
        "fullName": "Nguyen Van A",
        "avatarUrl": "https://...",
        "role": "Mentor",
        "isVerified": true,
        "isActive": true
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 45,
    "totalPages": 3
  }
}
```

### GET `/api/admin/users/{userId}`

**Mô t?**: Xem chi ti?t thông tin user

### POST `/api/admin/users` ? NEW

**Mô t?**: T?o user m?i (admin only)

**Request Body**:
```json
{
  "email": "newuser@example.com",
  "password": "password123",
  "fullName": "Nguyen Van New",
  "role": "Student",
  "phone": "0901234567",
  "gender": "Male",
  "school": "HCMUT",
  "major": "Computer Science",
  "bio": "Student bio here",
  "city": "Ho Chi Minh",
  "country": "Vietnam",
  "isVerified": false,
  "isActive": true
}
```

**Response**:
```json
{
  "success": true,
  "message": "User created successfully",
  "data": {
    "id": 151,
    "email": "newuser@example.com",
    "fullName": "Nguyen Van New",
    "role": "Student",
    "isVerified": false,
    "isActive": true,
    "createdAt": "2026-01-23T12:00:00Z"
  }
}
```

**Logic**:
- Ki?m tra email unique
- Hash password v?i BCrypt
- T?o User, UserProfile, Wallet
- N?u role = Mentor: t?o MentorProfile m?c ??nh
- Transaction safety

### PUT `/api/admin/users/{userId}` ? NEW

**Mô t?**: C?p nh?t thông tin user (admin only)

**Request Body**:
```json
{
  "email": "updated@example.com",
  "fullName": "Nguyen Van Updated",
  "phone": "0909999999",
  "gender": "Male",
  "school": "HCMUT",
  "major": "Software Engineering",
  "bio": "Updated bio",
  "city": "Ho Chi Minh",
  "country": "Vietnam",
  "isVerified": true,
  "isActive": true
}
```

**Response**:
```json
{
  "success": true,
  "message": "User updated successfully",
  "data": {
    "id": 151,
    "email": "updated@example.com",
    "fullName": "Nguyen Van Updated",
    "role": "Student",
    "isVerified": true,
    "isActive": true
  }
}
```

**Logic**:
- Không th? update admin account
- Ki?m tra email unique (n?u thay ??i)
- C?p nh?t User và UserProfile

### DELETE `/api/admin/users/{userId}` ? NEW

**Mô t?**: Xóa user (hard delete, admin only)

**Response**:
```json
{
  "success": true,
  "message": "User deleted successfully"
}
```

**Logic**:
- Không th? xóa admin account
- Không th? xóa n?u có active orders
- Không th? xóa n?u wallet balance > 0
- Cascade delete: UserProfile, MentorProfile, Wallet
- Transaction safety

**L?u ý**: ?ây là hard delete, dùng ban/deactivate cho soft delete.

### POST `/api/admin/users/{userId}/activate`

**Mô t?**: Kích ho?t tài kho?n user b? deactivate

### POST `/api/admin/users/{userId}/deactivate`

**Mô t?**: Vô hi?u hóa tài kho?n user (không th? login)

### POST `/api/admin/users/{userId}/ban`

**Mô t?**: Ban user v?nh vi?n

**Request Body**:
```json
{
  "reason": "Violated community guidelines"
}
```

### POST `/api/admin/users/{userId}/verify`

**Mô t?**: Xác minh tài kho?n user th? công

---

## ?? Category Management

### GET `/api/admin/categories`

**Mô t?**: L?y t?t c? categories (có c? hierarchy)

### GET `/api/admin/categories/{categoryId}`

**Mô t?**: Xem chi ti?t category và các category con

### POST `/api/admin/categories`

**Mô t?**: T?o category m?i

**Request Body**:
```json
{
  "name": "Web Development",
  "description": "Learn web development technologies",
  "parentId": null
}
```

### PUT `/api/admin/categories/{categoryId}`

**Mô t?**: C?p nh?t thông tin category

**Request Body**: Gi?ng POST

### DELETE `/api/admin/categories/{categoryId}`

**Mô t?**: Xóa category (không th? xóa n?u có child categories ho?c ?ang ???c s? d?ng)

---

## ?? Requests Management

### GET `/api/admin/requests`

**Mô t?**: Xem t?t c? requests v?i filter

**Query Parameters**:
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)
- `keyword` (string, optional) - Tìm ki?m trong title/description
- `categoryId` (long, optional) - Filter theo category
- `status` (RequestStatus enum, optional) - Filter theo status
- `studentId` (long, optional) - Filter theo student
- `fromDate` (DateTime, optional) - Filter t? ngày
- `toDate` (DateTime, optional) - Filter ??n ngày

**Example**: `/api/admin/requests?keyword=React&status=Open&pageSize=20`

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "title": "Need help with React project",
        "description": "I need a mentor for React and Redux...",
        "categoryName": "Web Development",
        "categoryId": 5,
        "expectedBudget": 500000,
        "expectedHours": 10,
        "status": "Open",
        "createdAt": "2026-01-20T08:00:00Z",
        "proposalCount": 5,
        "orderCount": 0,
        "studentId": 10,
        "studentName": "Nguyen Van B",
        "studentEmail": "student@example.com",
        "studentAvatar": "https://..."
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 120,
    "totalPages": 6
  }
}
```

### DELETE `/api/admin/requests/{requestId}`

**Mô t?**: Xóa request vi ph?m (không th? xóa n?u có active orders)

**Query Parameters**:
- `reason` (string, default: "Violated policies") - Lý do xóa

**Logic**:
- Ki?m tra không có active orders
- Xóa t?t c? proposals liên quan tr??c
- Xóa request

---

## ?? Proposals Management

### GET `/api/admin/proposals`

**Mô t?**: Xem t?t c? proposals v?i filter

**Query Parameters**:
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)
- `requestId` (long, optional) - Filter theo request
- `mentorId` (long, optional) - Filter theo mentor
- `status` (ProposalStatus enum, optional) - Filter theo status
- `fromDate` (DateTime, optional)
- `toDate` (DateTime, optional)

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "requestId": 5,
        "requestTitle": "Need help with React project",
        "price": 450000,
        "estimatedHours": 10,
        "message": "I have 5 years experience with React...",
        "status": "Pending",
        "createdAt": "2026-01-20T09:00:00Z",
        "mentorId": 3,
        "mentorName": "Tran Van C",
        "mentorEmail": "mentor@example.com",
        "mentorAvatar": "https://...",
        "studentId": 10,
        "studentName": "Nguyen Van B"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 95,
    "totalPages": 10
  }
}
```

### DELETE `/api/admin/proposals/{proposalId}`

**Mô t?**: Xóa proposal vi ph?m (không th? xóa n?u có active orders)

**Query Parameters**:
- `reason` (string, default: "Violated policies")

---

## ?? Orders Management

### GET `/api/admin/orders`

**Mô t?**: Xem t?t c? orders v?i filter nâng cao

**Query Parameters**:
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)
- `keyword` (string, optional) - Tìm trong request title
- `status` (OrderStatus enum, optional) - Filter theo status
- `billingType` (BillingType enum, optional) - Hourly/Package
- `studentId` (long, optional)
- `mentorId` (long, optional)
- `fromDate` (DateTime, optional)
- `toDate` (DateTime, optional)
- `hasDispute` (bool, optional) - Filter orders ?ang tranh ch?p

**Example**: `/api/admin/orders?status=Disputed&hasDispute=true`

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "requestTitle": "Need help with React project",
        "billingType": "Hourly",
        "agreedPrice": null,
        "agreedHourlyRate": 50000,
        "totalPrice": 500000,
        "status": "InProgress",
        "startedAt": "2026-01-21T10:00:00Z",
        "completedAt": null,
        "cancelledAt": null,
        "createdAt": "2026-01-21T09:00:00Z",
        "studentId": 10,
        "studentName": "Nguyen Van B",
        "studentEmail": "student@example.com",
        "mentorId": 3,
        "mentorName": "Tran Van C",
        "mentorEmail": "mentor@example.com",
        "hasEscrow": true,
        "escrowAmount": 500000,
        "escrowStatus": "Holding",
        "reportCount": 0
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 85,
    "totalPages": 9
  }
}
```

### GET `/api/admin/orders/{orderId}`

**Mô t?**: Xem chi ti?t order

### PUT `/api/admin/orders/{orderId}/status`

**Mô t?**: Admin can thi?p thay ??i status c?a order

**Request Body**:
```json
{
  "newStatus": "Cancelled",
  "note": "Cancelled due to dispute resolution",
  "notifyUsers": true
}
```

**Logic**:
- Admin có th? thay ??i b?t k? status nào
- T? ??ng c?p nh?t startedAt, completedAt, cancelledAt
- Ghi log vào order_details v?i prefix "[Admin Action]"
- G?i notification cho student và mentor (n?u notifyUsers = true)

### POST `/api/admin/orders/{orderId}/refund`

**Mô t?**: Admin hoàn ti?n t? escrow v? student

**Request Body**:
```json
{
  "amount": 500000,
  "reason": "Mentor did not deliver as promised",
  "fullRefund": true
}
```

**Logic**:
1. Ki?m tra order có escrow ?ang holding
2. Validate amount <= escrowAmount
3. Begin transaction:
   - C?p nh?t escrow status = Refunded
   - C?p nh?t order status = Cancelled
   - C?ng ti?n vào student wallet
   - T?o transaction record
   - Ghi log vào order_details
4. Commit transaction

**Response**:
```json
{
  "success": true,
  "message": "Refund of 500,000 VND processed successfully"
}
```

---

## ?? Community Posts Management

### GET `/api/admin/posts`

**Mô t?**: Xem t?t c? posts trong community

**Query Parameters**:
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)
- `keyword` (string, optional) - Tìm trong title/content
- `authorId` (long, optional) - Filter theo author
- `fromDate` (DateTime, optional)
- `toDate` (DateTime, optional)
- `isHidden` (bool, optional) - Filter posts b? ?n

**Response**:
```json
{...}
```

### DELETE `/api/admin/posts/{postId}`

**Mô t?**: Xóa post vi ph?m (xóa luôn t?t c? comments)

**Query Parameters**:
- `reason` (string, default: "Violated policies")

### POST `/api/admin/posts/{postId}/hide`

**Mô t?**: ?n post (không hi?n th? cho users, nh?ng không xóa)

**Request Body** (optional):
```json
{
  "reason": "Inappropriate content"
}
```

---

## ?? Comments Management

### GET `/api/admin/comments`

**Mô t?**: Xem t?t c? comments

**Query Parameters**:
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)
- `postId` (long, optional) - Filter theo post
- `authorId` (long, optional) - Filter theo author
- `keyword` (string, optional) - Tìm trong content
- `fromDate` (DateTime, optional)
- `toDate` (DateTime, optional)

### DELETE `/api/admin/comments/{commentId}`

**Mô t?**: Xóa comment vi ph?m

---

## ?? Transactions Management

### GET `/api/admin/transactions`

**Mô t?**: Xem t?t c? giao d?ch v?i filter chi ti?t

**Query Parameters**:
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 20)
- `userId` (long, optional) - Filter theo user
- `type` (TransactionType enum, optional) - Topup, Withdraw, EscrowHold, etc.
- `status` (TransactionStatus enum, optional) - Pending, Success, Failed
- `fromDate` (DateTime, optional)
- `toDate` (DateTime, optional)
- `minAmount` (decimal, optional)
- `maxAmount` (decimal, optional)

### POST `/api/admin/transactions/{transactionId}/verify`

**Mô t?**: Admin xác minh ho?c t? ch?i transaction pending

**Request Body**:
```json
{
  "approve": true,
  "note": "Bank transfer #123456 verified"
}
```

---

## ?? Wallets Management

### GET `/api/admin/wallets`

**Mô t?**: Xem t?t c? wallets v?i filter

### GET `/api/admin/wallets/{walletId}`

**Mô t?**: Xem chi ti?t wallet (bao g?m th?ng kê)

---

## ? Reviews Management

### GET `/api/admin/reviews`

**Mô t?**: Xem t?t c? reviews v?i filter

### DELETE `/api/admin/reviews/{reviewId}`

**Mô t?**: Xóa review không phù h?p (t? ??ng recalculate rating)

---

## ??? Hashtags Management

### GET `/api/admin/hashtags`

**Mô t?**: Xem t?t c? hashtags

### POST `/api/admin/hashtags`

**Mô t?**: T?o hashtag m?i

**Request Body**:
```json
{
  "name": "vue"
}
```

### DELETE `/api/admin/hashtags/{hashtagId}`

**Mô t?**: Xóa hashtag n?u không ???c s? d?ng

---

## ?? Reports Management

### GET `/api/admin/reports`

**Mô t?**: Xem t?t c? reports

### GET `/api/admin/reports/{reportId}`

**Mô t?**: Xem chi ti?t report

### POST `/api/admin/reports/{reportId}/handle`

**Mô t?**: X? lý report

**Request Body**:
```json
{
  "status": "resolved",
  "banUser": false
}
```

---

## ?? Error Responses

T?t c? endpoints ??u tr? v? error theo format:

```json
{
  "success": false,
  "message": "Error message here",
  "errors": ["Validation error 1", "Validation error 2"],
  "statusCode": 400
}
```

---

## ?? Authorization

T?t c? endpoints yêu c?u JWT token và role = Admin.

---

## ?? Technical Implementation

Xem `docs/ADMIN_IMPLEMENTATION_SUMMARY.md` ?? bi?t chi ti?t thay ??i code và logic.

---

**Version**: 1.0.0  
**Last Updated**: January 23, 2026

---

## ?? K?t n?i và dùng API t? React (Admin SPA)

Ph?n này h??ng d?n cách g?i các endpoints t? frontend React ?? ?? data lên UI và th?c hi?n CRUD.

L?u ý chung
- S? d?ng JWT Bearer token trong header `Authorization: Bearer {token}`.
- L?u token an toàn (ví d? `localStorage` cho SPA ??n gi?n) ho?c dùng HttpOnly cookie n?u mu?n b?o m?t cao h?n.
- Backend ?ã b?t CORS cho dev origins (127.0.0.1:3000 / 5173). N?u deploy, c?p nh?t CORS t??ng ?ng.

1) C?u hình axios (recommended)

```javascript
// src/api/axios.js
import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' }
});

// Attach token
api.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers['Authorization'] = `Bearer ${token}`;
  return config;
});

export default api;
```

2) L?y danh sách (example: requests)

```javascript
// src/hooks/useAdminRequests.js
import { useState, useEffect } from 'react';
import api from '../api/axios';

export default function useAdminRequests({ page = 1, pageSize = 10, keyword = '' }) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    api.get('/admin/requests', { params: { pageNumber: page, pageSize, keyword } })
      .then(res => { if (mounted) setData(res.data.data); })
      .catch(err => { if (mounted) setError(err.response?.data || err.message); })
      .finally(() => { if (mounted) setLoading(false); });
    return () => { mounted = false; };
  }, [page, pageSize, keyword]);

  return { data, loading, error };
}
```

Component s? d?ng:
```javascript
import React from 'react';
import useAdminRequests from '../hooks/useAdminRequests';

export default function RequestsList() {
  const { data, loading, error } = useAdminRequests({ page: 1, pageSize: 20 });

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {JSON.stringify(error)}</div>;

  return (
    <div>
      {data.items.map(r => (
        <div key={r.id}>{r.title} - {r.studentName}</div>
      ))}
    </div>
  );
}
```

3) T?o (POST), C?p nh?t (PUT), Xóa (DELETE)

```javascript
// Create request
async function createRequest(payload) {
  const res = await api.post('/requests', payload); // user endpoint, admin may use admin-specific
  return res.data;
}

// Admin delete request
async function adminDeleteRequest(requestId, reason = 'Violated policies') {
  const res = await api.delete(`/admin/requests/${requestId}`, { params: { reason } });
  return res.data;
}

// Update order status (admin)
async function adminUpdateOrderStatus(orderId, newStatus, note) {
  const payload = { newStatus, note };
  const res = await api.put(`/admin/orders/${orderId}/status`, payload);
  return res.data;
}
```

- Luôn x? lý HTTP status và thông báo l?i tr? v? t? API (res.data.message, res.data.errors).
- Sau create/update/delete, refresh data ho?c áp d?ng optimistic update ?? UI m??t.

4) Verify transaction (example)

```javascript
async function verifyTransaction(transactionId, approve, note) {
  const res = await api.post(`/admin/transactions/${transactionId}/verify`, { approve, note });
  return res.data;
}
```

5) Form handling & validation
- Dùng `react-hook-form` ho?c `formik` + `yup` ?? validate form tr??c khi g?i API.
- Hi?n th? l?i server (res.data.errors) n?u có.

6) Pagination UI
- S? d?ng response `pageNumber`, `pageSize`, `totalCount`, `totalPages` ?? hi?n th? paging control.
- G?i API v?i param `pageNumber` khi ng??i dùng chuy?n trang.

7) Authorization flow (login -> l?u token)
- Khi login thành công, l?u `accessToken` vào localStorage:
  `localStorage.setItem('accessToken', response.data.data.token)`
- Redirect t?i admin dashboard. Axios interceptor s? t? g?n token.

8) Error handling & UX
- V?i 401: redirect v? login.
- V?i 403: hi?n th? "Access denied".
- V?i 400/422: hi?n th? validation messages.
- Loading states, disabled buttons khi g?i form.

9) CORS & SignalR notes
- N?u dùng SignalR c?n truy?n token qua `access_token` query param khi connect hub.
- Backend ?ã c?u hình CORS cho dev origins; n?u g?p l?i CORS ki?m tra console network.

10) Tips cho CRUD table (Admin)
- Thêm confirm dialog tr??c khi delete.
- Show toast message sau m?i action (success/error).
- Provide server-side sorting/filtering (API h? tr? params).
- Use debounce for search input to reduce API calls.

---

N?u b?n mu?n, tôi có th?:
- Thêm các ví d? component React c? th? (List, CreateForm, EditForm, DeleteConfirm) cho endpoints b?n c?n.
- Ghi m?t small admin dashboard demo page (React) dùng axios và react-hook-form.

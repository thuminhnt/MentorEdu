# ? Fix: Tính n?ng Search Users ?ã ho?t ??ng

## V?n ?? ban ??u
- Ô search trên giao di?n UsersPage **không ho?t ??ng**
- Khi gõ keyword vào ô search, API không l?c users theo tên ho?c email
- Backend `GetUsersAsync` thi?u param `keyword` ?? filter

---

## Gi?i pháp ?ã implement

### 1. **Backend - AdminService.cs**
Thêm filter search theo email và full name:

```csharp
public async Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(
    int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null, string? keyword = null)
{
    var query = _unitOfWork.Users.Query();

    if (role.HasValue)
        query = query.Where(u => u.Role == role.Value);

    if (isActive.HasValue)
        query = query.Where(u => u.IsActive == isActive.Value);

    // ? Thêm filter keyword
    if (!string.IsNullOrWhiteSpace(keyword))
        query = query.Where(u => u.Email.Contains(keyword) || u.UserProfile.FullName.Contains(keyword));

    // ...rest of code...
}
```

### 2. **Backend - IAdminService.cs**
C?p nh?t signature:

```csharp
Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(
    int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null, string? keyword = null);
```

### 3. **Backend - AdminController.cs**
Thêm query param `keyword`:

```csharp
[HttpGet("users")]
public async Task<ActionResult<ApiResponse<PagedResponse<UserBasicDto>>>> GetUsers(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] UserRole? role = null,
    [FromQuery] bool? isActive = null,
    [FromQuery] string? keyword = null)  // ? Thêm param này
{
    var result = await _adminService.GetUsersAsync(pageNumber, pageSize, role, isActive, keyword);
    // ...
}
```

---

## Cách test

### Backend (Swagger ho?c Postman)
```http
GET /api/admin/users?keyword=john&pageNumber=1&pageSize=10
Authorization: Bearer {admin_token}
```

Response s? ch? tr? v? users có email ho?c full name ch?a "john".

### Frontend (UsersPage.jsx)
1. M? trang Users trong admin panel
2. Gõ t? khóa vào ô search (ví d?: "student", "mentor@example")
3. ??i 400ms (debounce)
4. API t? ??ng g?i v?i param `keyword`
5. Danh sách users ???c filter hi?n th?

---

## Frontend debounce (recommended)

Frontend **nên thêm debounce** ?? tránh g?i API liên t?c khi gõ phím. Code m?u:

```javascript
const [searchKeyword, setSearchKeyword] = useState('');
const [debouncedKeyword, setDebouncedKeyword] = useState('');

// Debounce 400ms
useEffect(() => {
  const handler = setTimeout(() => setDebouncedKeyword(searchKeyword), 400);
  return () => clearTimeout(handler);
}, [searchKeyword]);

// Fetch khi debouncedKeyword thay ??i
useEffect(() => {
  fetchUsers();
}, [pageNumber, pageSize, filterRole, filterStatus, debouncedKeyword]);

// Trong fetchUsers, dùng debouncedKeyword
const fetchUsers = async () => {
  const res = await adminApi.getUsers({
    pageNumber,
    pageSize,
    role: roleParam,
    isActive: statusParam,
    keyword: debouncedKeyword  // ? Dùng keyword ?ã debounce
  });
  // ...
};
```

---

## Search behavior

- **Case-insensitive**: Backend dùng `.Contains()` (SQL Server default collation là case-insensitive)
- **Partial match**: Keyword "john" s? match "John Doe", "johnny@example.com"
- **OR logic**: Search trong **email** HO?C **full name**
- **Combined filters**: Có th? k?t h?p search v?i role, status, pagination

---

## Ví d? API calls

```http
# Tìm users có "student" trong name ho?c email
GET /api/admin/users?keyword=student

# Tìm mentors active có "nguyen" trong name
GET /api/admin/users?keyword=nguyen&role=Mentor&isActive=true

# Tìm users có "@example.com" trong email (page 2)
GET /api/admin/users?keyword=@example.com&pageNumber=2&pageSize=20
```

---

## Performance notes

- `.Contains()` không dùng index ? v?i DB l?n (>10k users) nên:
  - Thêm full-text search index (SQL Server)
  - Ho?c dùng ElasticSearch cho search nhanh h?n
- V?i <1000 users, performance ?n

---

## Tóm t?t

? **Backend**: ?ã thêm filter keyword cho email + full name  
? **API**: Param `keyword` ?ã có trong endpoint GET `/api/admin/users`  
? **Frontend**: Có th? truy?n keyword, nên thêm debounce cho UX m??t  
? **Test**: Gõ t? khóa vào ô search ? danh sách t? ??ng filter  

Gi? search ho?t ??ng ??y ??! ??

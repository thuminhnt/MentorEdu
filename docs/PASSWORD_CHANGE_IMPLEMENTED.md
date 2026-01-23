# ? Password Change Implemented - Backend & Frontend

## Tóm t?t thay ??i

### Backend (.NET)
? **AdminUpdateUserDto.cs** - Thêm field Password (optional, validated)  
? **AdminService.cs** - Hash password n?u ???c cung c?p khi update user  
? **Validation** - Regex ki?m tra uppercase, lowercase, number  

### Frontend (React)
? **Ch?a có frontend validation** - C?n implement

---

## Backend Changes

### 1. DTO Validation (DTOs/Admin/AdminDtos.cs)

```csharp
public class AdminUpdateUserDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    // ? NEW: Password field (optional)
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(100, ErrorMessage = "Password must be less than 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
    public string? Password { get; set; }

    // ...existing fields...
}
```

### 2. Service Logic (Services/Implementations/AdminService.cs)

```csharp
public async Task<ServiceResult<UserDetailDto>> AdminUpdateUserAsync(long userId, AdminUpdateUserDto request)
{
    // ...existing validation...

    // ? Update password if provided
    if (!string.IsNullOrWhiteSpace(request.Password))
    {
        user.PasswordHash = HashPassword(request.Password);
    }

    _unitOfWork.Users.Update(user);
    // ...rest of code...
}

private static string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password);
}
```

---

## Password Validation Rules

### Backend (C# Attributes)
- ? **MinLength**: 6 characters
- ? **MaxLength**: 100 characters
- ? **Regex**: Must contain at least:
  - 1 uppercase letter (A-Z)
  - 1 lowercase letter (a-z)
  - 1 number (0-9)

### Ví d? valid passwords
```
Password1  ?
MyPass123  ?
Admin2024  ?
Test@1234  ?
```

### Ví d? invalid passwords
```
pass       ? (too short, no uppercase, no number)
password   ? (no uppercase, no number)
PASSWORD   ? (no lowercase, no number)
Password   ? (no number)
12345678   ? (no letters)
```

---

## API Endpoint Usage

### Request (PUT /api/admin/users/{userId})

**Case 1: Ch? update name (không ??i password)**
```json
{
  "email": "user@example.com",
  "fullName": "Updated Name",
  "isVerified": true,
  "isActive": true
}
```

**Case 2: Update name + password**
```json
{
  "email": "user@example.com",
  "fullName": "Updated Name",
  "password": "NewPass123",  // ? Password m?i (optional)
  "isVerified": true,
  "isActive": true
}
```

### Response (Success)
```json
{
  "success": true,
  "message": "User updated successfully",
  "data": {
    "id": 6,
    "email": "user@example.com",
    "fullName": "Updated Name",
    "role": "Student",
    // ...
  }
}
```

### Response (Error - Invalid Password)
```json
{
  "success": false,
  "message": "Invalid request data",
  "errors": [
    "Password must be at least 6 characters",
    "Password must contain at least one uppercase letter, one lowercase letter, and one number"
  ],
  "statusCode": 400
}
```

---

## Frontend Implementation (TODO)

### C?n b? sung vào UsersPage.jsx:

1. **Real-time validation khi nh?p password**
2. **Hi?n th? l?i d??i ô input**
3. **Block submit n?u password invalid**
4. **Show backend error message chi ti?t**

Code m?u ?ã có trong `docs/PASSWORD_VALIDATION_GUIDE.md`.

---

## Testing

### Backend Test (Swagger / Postman)

```http
PUT /api/admin/users/6
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "email": "student@example.com",
  "fullName": "Test Student",
  "password": "NewPass123",
  "isVerified": true,
  "isActive": true
}
```

### Frontend Test (UsersPage)

1. M? modal Update User
2. Nh?p password m?i (ví d?: "Test@123")
3. Submit
4. Ki?m tra:
   - ? Backend nh?n ?úng password
   - ? Password ???c hash và l?u DB
   - ? User có th? login v?i password m?i

---

## Security Notes

? **Password hashing**: BCrypt v?i salt t? ??ng  
? **Password không b?t bu?c**: N?u không nh?p = gi? nguyên password c?  
? **Validation 2 t?ng**: Frontend (UX) + Backend (b?o m?t)  
? **Error message rõ ràng**: User bi?t chính xác password sai ? ?âu  

---

## Next Steps

1. ? **Backend** - ?Ã XONG (DTO validation + Service logic)
2. ? **Frontend** - Ch?a có validation, c?n implement:
   - Add state `passwordError`
   - Add function `validatePassword()`
   - Add onChange handler with validation
   - Add UI hi?n th? l?i (màu ??, text d??i input)
   - Block submit khi password invalid
3. ? **Testing** - Test th? công trên Swagger/Postman
4. ? **Unit tests** - Ch?a có test cho password validation

---

## Cách s? d?ng (Admin)

1. **M? trang Users** trong admin panel
2. **Click icon Edit** ? row user mu?n ??i password
3. **Nh?p password m?i** vào ô Password (?? tr?ng n?u không ??i)
4. **Click Update**
5. **Ki?m tra**: User có th? login v?i password m?i

---

?ã hoàn t?t implement backend! Frontend s? ???c b? sung trong b??c ti?p theo.

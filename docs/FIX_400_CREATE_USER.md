# Fix l?i 400 Bad Request khi Create User

## Nguyên nhân

Backend .NET Core expect `role` là **enum s?** (UserRole), không ph?i string:

```csharp
public class AdminCreateUserDto
{
    [Required]
    public UserRole Role { get; set; }  // 0=Student, 1=Mentor, 2=Admin
}
```

Nh?ng frontend ?ang g?i:
```json
{
  "role": "Student"  // ? String
}
```

Backend mu?n:
```json
{
  "role": 0  // ? Number (Student)
}
```

---

## Gi?i pháp

### 1. T?o helper function convert role

Thêm vào `src/api/adminApi.js`:

```javascript
// Role mapping: string -> number
const roleToNumber = {
  'student': 0,
  'mentor': 1,
  'admin': 2
};

// Helper to convert role string to enum number
function mapRoleForAPI(roleString) {
  const normalized = roleString?.toLowerCase();
  return roleToNumber[normalized] ?? 0; // default Student
}
```

### 2. Update `createUser` method

```javascript
// Create user (admin)
createUser(data) {
  const payload = {
    email: data.email,
    password: data.password,
    fullName: data.fullName,
    role: mapRoleForAPI(data.role),  // Convert to number
    phone: data.phone || null,
    gender: data.gender || null,
    school: data.school || null,
    major: data.major || null,
    bio: data.bio || null,
    city: data.city || null,
    country: data.country || null,
    isVerified: data.isVerified ?? false,
    isActive: data.isActive ?? true
  };
  return axiosClient.post("/api/admin/users", payload);
}
```

### 3. Update `updateUser` method

```javascript
// Update user (admin)
updateUser(userId, data) {
  const payload = {
    email: data.email,
    fullName: data.fullName,
    phone: data.phone || null,
    gender: data.gender || null,
    school: data.school || null,
    major: data.major || null,
    bio: data.bio || null,
    city: data.city || null,
    country: data.country || null,
    isVerified: data.isVerified ?? false,
    isActive: data.isActive ?? true
  };
  // Note: Role cannot be updated in UpdateUserDto
  return axiosClient.put(`/api/admin/users/${userId}`, payload);
}
```

---

## Full code `src/api/adminApi.js`

```javascript
import axiosClient from './axios';

// Role mapping
const roleToNumber = {
  'student': 0,
  'mentor': 1,
  'admin': 2
};

function mapRoleForAPI(roleString) {
  const normalized = roleString?.toLowerCase();
  return roleToNumber[normalized] ?? 0;
}

const adminApi = {
  // ...existing methods...

  createUser(data) {
    const payload = {
      email: data.email,
      password: data.password,
      fullName: data.fullName,
      role: mapRoleForAPI(data.role),
      phone: data.phone || null,
      gender: data.gender || null,
      school: data.school || null,
      major: data.major || null,
      bio: data.bio || null,
      city: data.city || null,
      country: data.country || null,
      isVerified: data.isVerified ?? false,
      isActive: data.isActive ?? true
    };
    return axiosClient.post("/api/admin/users", payload);
  },

  updateUser(userId, data) {
    const payload = {
      email: data.email,
      fullName: data.fullName,
      phone: data.phone || null,
      gender: data.gender || null,
      school: data.school || null,
      major: data.major || null,
      bio: data.bio || null,
      city: data.city || null,
      country: data.country || null,
      isVerified: data.isVerified ?? false,
      isActive: data.isActive ?? true
    };
    return axiosClient.put(`/api/admin/users/${userId}`, payload);
  },

  deleteUser(userId) {
    return axiosClient.delete(`/api/admin/users/${userId}`);
  }
};

export default adminApi;
```

---

## Test

1. M? UsersPage
2. Click "Create User"
3. ?i?n form v?i role "Student"
4. Submit

Backend s? nh?n:
```json
{
  "email": "student@example.com",
  "password": "student123",
  "fullName": "student",
  "role": 0,  // ? Number
  "isVerified": false,
  "isActive": true
}
```

---

## L?u ý quan tr?ng

### Backend validation rules (AdminCreateUserDto):

**Required fields**:
- `email` (valid email format)
- `password` (min 6 characters)
- `fullName` (max 100 characters)
- `role` (0, 1, or 2)

**Optional fields**:
- `phone` (valid phone format if provided)
- `gender`, `school`, `major`, `bio`, `city`, `country`
- `isVerified` (default: false)
- `isActive` (default: true)

### Update user không cho phép thay ??i role

Backend `AdminUpdateUserDto` **không có field role**, nên không th? update role sau khi t?o.

---

## Error handling trong UsersPage.jsx

?? show error message t? backend:

```javascript
async function submitCreate() {
  try {
    const response = await adminApi.createUser(formData);
    showToast(response.message || 'User created successfully!', 'success');
    setIsCreateModalOpen(false);
    fetchUsers();
  } catch (err) {
    // Show backend validation errors
    const errorMsg = err.response?.data?.message || 
                     err.response?.data?.errors?.[0] || 
                     err.message ||
                     'Failed to create user';
    showToast(errorMsg, 'error');
    console.error('Failed to create user', err);
  }
}
```

---

## Tóm t?t

? **Fix**: Convert role string ? number tr??c khi g?i API  
? **Validate**: ??m b?o ?? required fields (email, password, fullName, role)  
? **Error**: Show backend validation messages chi ti?t  
? **Optional**: G?i null cho các field không dùng  

Sau khi apply fix này, ch?c n?ng Create User s? ho?t ??ng!

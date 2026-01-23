# React Admin — H??ng d?n qu?n lý User (t?p ??t tên rõ ràng)

M?c ?ích: Tài li?u này mô t? lu?ng end-to-end ?? hi?n th? và qu?n lý `User` t? frontend React s? d?ng API `/api/admin/users`. File m?u, c?u trúc và l?u ý ??t tên file ???c cung c?p ?? b?n copy vào project.

---

## T?ng quan lu?ng d? li?u (User management)
1. Admin ??ng nh?p ? nh?n JWT ? l?u token (ví d? `localStorage`).
2. Axios interceptor t? g?n token vào header m?i request.
3. Component `UsersList` g?i hook `useAdminUsers` ? hook g?i GET `/api/admin/users` ? nh?n `PagedResponse`.
4. UI hi?n th? danh sách, paging, tìm ki?m.
5. Khi admin thao tác (activate/deactivate/ban/verify)
   - G?i API t??ng ?ng (POST endpoints)
   - Sau khi thành công, g?i `refresh()` ?? refetch d? li?u ho?c áp d?ng optimistic update.

---

## Th? m?c và tên file (?? xu?t, d? hi?u)
- `src/api/axios.js` — c?u hình axios (interceptor token)
- `src/api/admin.js` — helper g?i API admin (get users, actions)
- `src/hooks/useAdminUsers.js` — hook fetch/pagination/search cho users
- `src/components/admin/UsersList.jsx` — component hi?n th? b?ng users + actions
- `src/components/admin/UserDetailModal.jsx` — modal/side panel hi?n th? chi ti?t user
- `src/components/common/ConfirmDialog.jsx` — reusable confirm dialog
- `src/components/common/Toast.jsx` — reusable toast notifications

> Note: ??t tên file theo pattern `category/FeatureName.ext` (ví d? `admin/UsersList.jsx`) d? hi?u khi scale.

---

## N?i dung file m?u (tóm t?t)
- `axios.js`
  - Base URL `/api`
  - Request interceptor g?n `Authorization: Bearer {token}`
  - Response interceptor x? lý 401 global

- `admin.js` (API helper)
  - `getAdminUsers({ page, pageSize, keyword, role, isActive })`
  - `getAdminUserDetail(userId)`
  - `adminActivateUser(userId)`
  - `adminDeactivateUser(userId)`
  - `adminBanUser(userId, reason)`
  - `adminVerifyUser(userId)`

- `useAdminUsers.js` (hook)
  - Tr? v? `{ data, loading, error, refresh }`
  - Th?c hi?n fetch v?i params và set state

- `UsersList.jsx` (component)
  - Input search + debounce
  - B?ng users (ID, name, email, role, actions)
  - Paging control dùng `pageNumber`, `pageSize`, `totalCount`
  - Buttons: View, Activate, Deactivate, Ban, Verify
  - Confirm dialog tr??c khi destructive actions
  - Hi?n th? toast thông báo

---

## Các b??c chi ti?t (g?i ý implement)
1. T?o `src/api/axios.js` và ki?m tra interceptor.
2. T?o `src/api/admin.js` v?i các helper; luôn return `res.data`.
3. T?o `src/hooks/useAdminUsers.js` ?? encapsulate fetch logic.
4. T?o `src/components/admin/UsersList.jsx`:
   - import hook và helper
   - implement search input (debounced)
   - render table và action handlers
   - g?i `refresh()` sau action thành công
5. T?o `UserDetailModal.jsx` ?? show chi ti?t khi b?m View (g?i `getAdminUserDetail`).
6. T?o reusable `ConfirmDialog` và `Toast` ?? UX nh?t quán.

---

## X? lý l?i và UX
- Loading: show spinner khi loading
- Empty: show message "No users"
- Error: hi?n th? l?i server (`res.data.message` ho?c `res.data.errors`)
- 401: redirect v? login
- 403: show "Access denied"
- Disable buttons khi request ?ang g?i
- S? d?ng toast thay vì alert cho production

---

## An toàn token & b?o m?t
- ?u tiên dùng HttpOnly cookie n?u có backend support; n?u không, `localStorage` là tùy ch?n SPA nh?ng kém an toàn h?n.
- Xóa token khi nh?n 401 global.

---

## Testing nhanh
- T?o admin test account, login, l?u token.
- Truy c?p trang UsersList, ki?m tra list hi?n th?.
- Th? search, paging.
- Th? Activate/Deactivate/Ban/Verify và ki?m tra UI refresh.

---

## Ghi chú v? tên file (quan tr?ng)
- Dùng tên rõ ràng, prefix `admin` cho admin components: `admin/UsersList.jsx`, `admin/UserDetailModal.jsx`.
- Helpers ??t trong `src/api/` v?i tên mô t?: `admin.js`.
- Hooks ??t trong `src/hooks/` v?i tên `use...`.
- Common components ??t trong `src/components/common/`.

---

N?u mu?n, mình có th? t?o các file m?u tr?c ti?p trong workspace (React) theo c?u trúc trên. B?n mu?n mình t?o nh?ng file nào tr??c? (ví d?: `axios.js`, `api/admin.js`, `hooks/useAdminUsers.js`, `components/admin/UsersList.jsx`).

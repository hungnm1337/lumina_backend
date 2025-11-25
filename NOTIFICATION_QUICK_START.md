# Hệ thống Thông báo - Hướng dẫn Nhanh

## ✅ Đã hoàn thành

### Backend (.NET Core 8.0)
- ✅ DTOs: 4 files cho Notification và UserNotification
- ✅ Repositories: 4 files (interfaces + implementations)
- ✅ Services: 4 files với logic tự động broadcast
- ✅ Controllers: 2 files (Admin + User)
- ✅ Migration script: Tạo 2 bảng với foreign keys
- ✅ DI registration trong Program.cs

### Frontend (Angular)
- ✅ Admin Service: CRUD operations cho admin
- ✅ Admin Component: Giao diện giống Season Management
- ✅ User Component: Giao diện giống Facebook (dark theme)
- ✅ Routing: Đã config cho cả admin và user
- ✅ Không có lỗi compile

---

## 🚀 Cách chạy (Quick Start)

### 1. Chạy Migration Database

```sql
-- Mở file này trong SQL Server Management Studio và Execute:
lumina_backend/Migrations/Migration_2025_01_25_NotificationSystem.sql
```

**Kết quả:** Tạo 2 bảng `Notification` và `UserNotification`

### 2. Chạy Backend

```powershell
cd d:\DA25\lumina_backend\lumina
dotnet run
```

### 3. Chạy Frontend

```powershell
cd d:\DA25\lumina_frontend\lumina
npm start
```

---

## 🎯 Cách sử dụng

### Với Admin

**URL:** `http://localhost:4200/admin/notifications`

**Chức năng:**
1. **Xem danh sách** thông báo (có phân trang)
2. **Tạo mới** thông báo:
   - Click "Tạo mới"
   - Điền Title, Content
   - Check "Hiển thị" nếu muốn active
   - Click "Tạo mới" → **Tự động gửi đến TẤT CẢ user!**
3. **Sửa** thông báo: Click icon bút chì
4. **Xem chi tiết**: Click icon mắt
5. **Xóa** thông báo: Click icon thùng rác

### Với User

**URL:** `http://localhost:4200/notifications`

**Chức năng:**
1. **Xem tất cả** thông báo của mình
2. **Filter theo tab**:
   - "Tất cả": Hiện hết
   - "Chưa đọc": Chỉ hiện chưa đọc (có badge số lượng)
3. **Đánh dấu đã đọc**:
   - Click vào 1 thông báo → đánh dấu cái đó
   - Click "Đánh dấu tất cả đã đọc" → đánh dấu hết
4. **Blue dot** bên cạnh thông báo chưa đọc
5. **Section "Mới"** cho chưa đọc, **"Trước đó"** cho đã đọc

---

## 📡 API Endpoints

### Admin APIs
```
GET    /api/admin/notification?page=1&pageSize=10  # Lấy danh sách
GET    /api/admin/notification/{id}                # Lấy chi tiết
POST   /api/admin/notification                     # Tạo mới → auto broadcast
PUT    /api/admin/notification/{id}                # Cập nhật
DELETE /api/admin/notification/{id}                # Xóa → cascade delete
```

### User APIs
```
GET /api/usernotification                          # Lấy thông báo của tôi
GET /api/usernotification/unread-count             # Đếm số chưa đọc
PUT /api/usernotification/{uniqueId}/read          # Đánh dấu đã đọc
```

---

## 🗄️ Database Tables

### Notification
- NotificationID (PK)
- Title
- Content
- IsActive
- CreatedAt, UpdatedAt

### UserNotification
- UniqueID (PK)
- UserID (FK → Users)
- NotificationID (FK → Notification)
- IsRead
- CreatedAt

**Cascade Delete:** Xóa Notification → xóa tất cả UserNotification liên quan

---

## 🔥 Tính năng nổi bật

1. **Auto Broadcast**: Admin tạo 1 thông báo → Hệ thống tự động tạo N UserNotification (N = số user)

2. **Real-time-ish**: Frontend poll API mỗi 30s để check thông báo mới

3. **Responsive**: Giao diện tối ưu cho cả desktop và mobile

4. **Dark Theme**: User notification page dùng màu tối giống Facebook

5. **Pagination**: Admin page có phân trang, không load hết vào RAM

6. **Type-safe**: Dùng DTOs riêng cho mỗi operation (Create, Update, Read)

7. **Authorization**: Admin APIs chỉ cho role Admin, User APIs extract userId từ JWT

8. **Empty States**: Hiển thị thông báo khi không có data

9. **Loading States**: Spinner khi đang load

10. **Error Handling**: Hiển thị lỗi rõ ràng cho user

---

## 🧪 Test nhanh

### Test 1: Admin tạo → User nhận
1. Login admin → `/admin/notifications`
2. Tạo thông báo "Test 123"
3. Logout → Login user bất kỳ → `/notifications`
4. ✅ Phải thấy "Test 123" trong section "Mới"

### Test 2: User đánh dấu đã đọc
1. Click vào thông báo "Test 123"
2. ✅ Blue dot biến mất
3. ✅ Chuyển sang section "Trước đó"
4. ✅ Badge "Chưa đọc" giảm đi 1

### Test 3: Admin xóa → User không thấy
1. Admin xóa thông báo "Test 123"
2. User reload page
3. ✅ Không còn thấy "Test 123" nữa

---

## 📂 Files quan trọng

### Backend
```
DataLayer/DTOs/Notification/
  - NotificationDTO.cs
  - CreateNotificationDTO.cs
  - UpdateNotificationDTO.cs
  - UserNotificationDTO.cs

ServiceLayer/Notification/
  - NotificationService.cs (⭐ Logic auto broadcast)

Controllers/
  - NotificationController.cs (Admin)
  - UserNotificationController.cs (User)

Migrations/
  - Migration_2025_01_25_NotificationSystem.sql (⭐ Chạy file này trước!)
```

### Frontend
```
Services/Notification/
  - notification.service.ts (User service)
  - admin-notification.service.ts (Admin service)

Views/Admin/notification-management/
  - notification-management.component.* (Admin CRUD UI)

Views/User/notifications-page/
  - notifications-page.component.* (User notification list)
```

---

## ⚠️ Lưu ý quan trọng

1. **Phải chạy migration trước** khi test!
2. **Backend phải đang chạy** thì frontend mới gọi được API
3. **Login với đúng role**: Admin để vào `/admin/notifications`, User để vào `/notifications`
4. **Token hết hạn**: Logout và login lại nếu gặp lỗi 401
5. **CORS**: Nếu lỗi CORS, check `Program.cs` có config đúng origin không

---

## 🎉 Kết luận

Hệ thống đã hoàn chỉnh và sẵn sàng sử dụng!

- ✅ Backend API hoạt động
- ✅ Frontend UI đẹp mắt
- ✅ Không có lỗi compile
- ✅ Đã test flow: Admin tạo → User nhận → Đánh dấu đã đọc

**Xem hướng dẫn chi tiết:** `NOTIFICATION_SYSTEM_COMPLETE_GUIDE.md`

**Chúc bạn làm việc hiệu quả! 🚀**

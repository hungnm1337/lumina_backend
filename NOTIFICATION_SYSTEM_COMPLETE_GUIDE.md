# Notification System - Complete Implementation Guide

## Tổng quan hệ thống

Hệ thống thông báo toàn diện với 2 vai trò:

### 🔐 **Admin**
- Quản lý thông báo với CRUD đầy đủ
- Giao diện giống quản lý mùa (Season Management)
- Sidebar navigation + modal dialogs
- Khi tạo thông báo mới → tự động gửi đến **TẤT CẢ người dùng**

### 👤 **User**
- Xem tất cả thông báo của mình
- Phân loại theo tab: Tất cả / Chưa đọc
- Giao diện giống Facebook (dark theme)
- Đánh dấu đã đọc (từng cái hoặc tất cả)
- Hiển thị badge số thông báo chưa đọc

---

## 📁 Cấu trúc File đã tạo

### Backend (.NET Core)

```
lumina_backend/
├── DataLayer/DTOs/Notification/
│   ├── NotificationDTO.cs              # DTO đọc dữ liệu
│   ├── CreateNotificationDTO.cs        # DTO tạo thông báo (validation)
│   ├── UpdateNotificationDTO.cs        # DTO cập nhật (partial)
│   └── UserNotificationDTO.cs          # DTO cho người dùng xem
│
├── RepositoryLayer/Notification/
│   ├── INotificationRepository.cs      # Interface repo thông báo
│   ├── NotificationRepository.cs       # CRUD + pagination
│   ├── IUserNotificationRepository.cs  # Interface repo user-notification
│   └── UserNotificationRepository.cs   # Quản lý quan hệ user-notification
│
├── ServiceLayer/Notification/
│   ├── INotificationService.cs         # Interface service thông báo
│   ├── NotificationService.cs          # Logic: tạo → auto broadcast
│   ├── IUserNotificationService.cs     # Interface service user
│   └── UserNotificationService.cs      # Logic đánh dấu đã đọc, lấy danh sách
│
├── Controllers/
│   ├── NotificationController.cs       # API admin: /api/admin/notification
│   └── UserNotificationController.cs   # API user: /api/usernotification
│
└── Migrations/
    └── Migration_2025_01_25_NotificationSystem.sql  # Script tạo bảng

```

### Frontend (Angular)

```
lumina_frontend/lumina/src/app/
├── Services/Notification/
│   ├── notification.service.ts         # Service user (đã có sẵn)
│   └── admin-notification.service.ts   # Service admin CRUD (MỚI TẠO)
│
├── Views/Admin/notification-management/
│   ├── notification-management.component.ts     # Admin component
│   ├── notification-management.component.html   # Template CRUD
│   └── notification-management.component.css    # Style sidebar layout
│
└── Views/User/notifications-page/
    ├── notifications-page.component.ts          # User component
    ├── notifications-page.component.html        # Template Facebook-style
    └── notifications-page.component.css         # Dark theme styling

```

---

## 🚀 Cách chạy hệ thống

### Bước 1: Chạy Migration Database

1. Mở **SQL Server Management Studio** (SSMS)
2. Connect đến database `LuminaSystem`
3. Mở file: `lumina_backend/Migrations/Migration_2025_01_25_NotificationSystem.sql`
4. Execute script (F5)

**Script sẽ tạo:**
- Bảng `Notification`: Lưu thông báo hệ thống
- Bảng `UserNotification`: Quan hệ user-notification (1 thông báo → N user)
- Index cho hiệu suất truy vấn

**Kiểm tra migration thành công:**
```sql
-- Check tables created
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('Notification', 'UserNotification')

-- Check sample data
SELECT COUNT(*) FROM Notification
SELECT COUNT(*) FROM UserNotification
```

### Bước 2: Chạy Backend

```powershell
cd d:\DA25\lumina_backend\lumina
dotnet run
```

Backend sẽ chạy trên: `https://localhost:7216` (hoặc port đã cấu hình)

**Test API:**
```bash
# Test health
GET https://localhost:7216/api/health

# Test admin notification (cần token admin)
GET https://localhost:7216/api/admin/notification?page=1&pageSize=10
```

### Bước 3: Chạy Frontend

```powershell
cd d:\DA25\lumina_frontend\lumina
npm start
# hoặc
ng serve
```

Frontend sẽ chạy trên: `http://localhost:4200`

---

## 🎨 Giao diện & Routing

### Admin Routes

**URL:** `http://localhost:4200/admin/notifications`

**Route config:**
```typescript
// admin-routing.module.ts
{
  path: 'notifications',
  component: NotificationManagementComponent,
  data: { title: 'Quản lý thông báo' }
}
```

**Tính năng:**
- ✅ Sidebar navigation (giống season management)
- ✅ Bảng danh sách thông báo (ID, Tiêu đề, Nội dung, Trạng thái, Thời gian)
- ✅ Phân trang (10 items/page)
- ✅ Modal tạo mới (Title + Content + IsActive checkbox)
- ✅ Modal chỉnh sửa (partial update)
- ✅ Modal xem chi tiết
- ✅ Modal xác nhận xóa
- ✅ Loading states & error handling
- ✅ Success/Error alerts

**Khi tạo thông báo mới:**
1. Admin điền form (title, content)
2. Click "Tạo mới"
3. Backend:
   - Tạo record trong `Notification` table
   - Lấy tất cả `UserID` từ `Users` table
   - Tạo N records trong `UserNotification` (N = số user)
4. Frontend: Hiển thị success message + reload list

### User Routes

**URL:** `http://localhost:4200/notifications`

**Route config:**
```typescript
// app.routes.ts
{
  path: 'notifications',
  loadComponent: () => import('./Views/User/notifications-page/...'),
  canActivate: [AuthGuard]
}
```

**Tính năng:**
- ✅ Dark theme (giống Facebook)
- ✅ Header "Thông báo"
- ✅ Tabs: "Tất cả" / "Chưa đọc" (badge số lượng)
- ✅ Button "Đánh dấu tất cả đã đọc"
- ✅ Section "Mới" (unread notifications)
- ✅ Section "Trước đó" (read notifications)
- ✅ Icon bell gradient xanh dương
- ✅ Blue dot cho thông báo chưa đọc
- ✅ Relative time (vừa xong, 5 phút, 2 giờ, 3 ngày...)
- ✅ Click notification → đánh dấu đã đọc
- ✅ Empty state khi không có thông báo

---

## 📡 API Endpoints

### Admin APIs

**Base URL:** `/api/admin/notification`  
**Authorization:** `[Authorize(Roles = "Admin")]`

#### 1. Get All Notifications (Paginated)
```http
GET /api/admin/notification?page=1&pageSize=10
Authorization: Bearer {admin_token}

Response 200:
{
  "items": [
    {
      "notificationId": 1,
      "title": "Cập nhật hệ thống",
      "content": "Hệ thống sẽ bảo trì vào 10h ngày mai",
      "isActive": true,
      "createdAt": "2025-01-25T10:30:00Z",
      "updatedAt": "2025-01-25T10:30:00Z"
    }
  ],
  "total": 25,
  "page": 1,
  "pageSize": 10
}
```

#### 2. Get Notification by ID
```http
GET /api/admin/notification/{id}
Authorization: Bearer {admin_token}

Response 200: NotificationDTO (single object)
```

#### 3. Create Notification
```http
POST /api/admin/notification
Authorization: Bearer {admin_token}
Content-Type: application/json

Body:
{
  "title": "Thông báo mới",
  "content": "Nội dung thông báo...",
  "isActive": true
}

Response 201: NotificationDTO (created object)
```

**⚠️ LƯU Ý:** API này tự động tạo UserNotification cho TẤT CẢ user trong hệ thống!

#### 4. Update Notification
```http
PUT /api/admin/notification/{id}
Authorization: Bearer {admin_token}
Content-Type: application/json

Body (partial update):
{
  "title": "Tiêu đề mới",  // optional
  "content": "Nội dung mới",  // optional
  "isActive": false  // optional
}

Response 200: NotificationDTO (updated object)
```

#### 5. Delete Notification
```http
DELETE /api/admin/notification/{id}
Authorization: Bearer {admin_token}

Response 204: No Content
```

**⚠️ LƯU Ý:** Xóa Notification sẽ tự động xóa tất cả UserNotification liên quan (cascade delete)!

---

### User APIs

**Base URL:** `/api/usernotification`  
**Authorization:** `[Authorize]` (any authenticated user)

#### 1. Get My Notifications
```http
GET /api/usernotification
Authorization: Bearer {user_token}

Response 200:
[
  {
    "uniqueId": 123,
    "userId": 456,
    "notificationId": 1,
    "title": "Thông báo mới",
    "content": "Nội dung...",
    "isRead": false,
    "createdAt": "2025-01-25T10:30:00Z"
  }
]
```

#### 2. Get Unread Count
```http
GET /api/usernotification/unread-count
Authorization: Bearer {user_token}

Response 200:
{
  "unreadCount": 5
}
```

#### 3. Mark as Read
```http
PUT /api/usernotification/{uniqueId}/read
Authorization: Bearer {user_token}

Response 204: No Content
```

---

## 🗄️ Database Schema

### Table: Notification

| Column          | Type          | Constraints           |
|----------------|---------------|-----------------------|
| NotificationID | INT           | PRIMARY KEY, IDENTITY |
| Title          | NVARCHAR(200) | NOT NULL             |
| Content        | NVARCHAR(MAX) | NOT NULL             |
| IsActive       | BIT           | NOT NULL, DEFAULT 1  |
| CreatedAt      | DATETIME      | NOT NULL, DEFAULT NOW|
| UpdatedAt      | DATETIME      | NOT NULL, DEFAULT NOW|

**Index:** `IX_Notification_CreatedAt` (DESC)

### Table: UserNotification

| Column          | Type     | Constraints           |
|----------------|----------|-----------------------|
| UniqueID       | INT      | PRIMARY KEY, IDENTITY |
| UserID         | INT      | NOT NULL, FK → Users  |
| NotificationID | INT      | NOT NULL, FK → Notification |
| IsRead         | BIT      | NOT NULL, DEFAULT 0   |
| CreatedAt      | DATETIME | NOT NULL, DEFAULT NOW |

**Indexes:**
- `IX_UserNotification_UserID_IsRead`
- `IX_UserNotification_NotificationID`

**Foreign Keys:**
- `FK_UserNotification_Users` → CASCADE on delete
- `FK_UserNotification_Notification` → CASCADE on delete

---

## 🧪 Cách test hệ thống

### Test 1: Admin tạo thông báo

1. Login với tài khoản admin
2. Navigate: `http://localhost:4200/admin/notifications`
3. Click "Tạo mới"
4. Điền form:
   - Title: "Thông báo test"
   - Content: "Đây là thông báo thử nghiệm"
   - IsActive: checked
5. Click "Tạo mới" trong modal
6. **Kỳ vọng:**
   - Success message hiển thị
   - Thông báo xuất hiện trong bảng
   - Database: 1 record trong `Notification`, N records trong `UserNotification`

### Test 2: User nhận thông báo

1. Logout admin, login với tài khoản user bất kỳ
2. Navigate: `http://localhost:4200/notifications`
3. **Kỳ vọng:**
   - Thấy thông báo "Thông báo test" trong section "Mới"
   - Badge "Chưa đọc" hiển thị số 1
   - Blue dot bên cạnh notification

### Test 3: User đánh dấu đã đọc

1. Click vào notification "Thông báo test"
2. **Kỳ vọng:**
   - Blue dot biến mất
   - Notification chuyển sang section "Trước đó"
   - Badge "Chưa đọc" giảm xuống 0
   - Database: `IsRead` = 1 trong `UserNotification`

### Test 4: Admin chỉnh sửa thông báo

1. Login admin
2. Click nút "Sửa" trên thông báo
3. Thay đổi title thành "Thông báo đã cập nhật"
4. Click "Cập nhật"
5. **Kỳ vọng:**
   - Success message
   - Title mới hiển thị trong bảng
   - User vẫn nhìn thấy title mới khi reload

### Test 5: Admin xóa thông báo

1. Click nút "Xóa" trên thông báo
2. Confirm xóa trong modal
3. **Kỳ vọng:**
   - Success message
   - Thông báo biến mất khỏi bảng admin
   - User không còn thấy thông báo này nữa
   - Database: records trong cả 2 bảng đều bị xóa (cascade)

### Test 6: Pagination

1. Tạo hơn 10 thông báo
2. **Kỳ vọng:**
   - Chỉ hiển thị 10 items
   - Nút "Trang sau" enabled
   - Click "Trang sau" → load page 2

### Test 7: Tab filtering (User)

1. User có 5 thông báo: 2 đã đọc, 3 chưa đọc
2. Click tab "Chưa đọc"
3. **Kỳ vọng:**
   - Chỉ hiển thị 3 thông báo chưa đọc
   - Badge hiển thị số 3
4. Click tab "Tất cả"
5. **Kỳ vọng:**
   - Hiển thị cả 5 thông báo (section "Mới" + "Trước đó")

---

## 🔧 Cấu hình & Tuỳ chỉnh

### Backend Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=LuminaSystem;..."
  }
}
```

**Program.cs (DI đã đăng ký):**
```csharp
// Notification Services
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
builder.Services.AddScoped<IUserNotificationService, UserNotificationService>();
```

### Frontend Configuration

**environment.ts:**
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7216/api'  // Backend API base URL
};
```

**Polling interval (tự động check thông báo mới):**

File: `notification.service.ts`
```typescript
// Poll for new notifications every 30 seconds
interval(30000)  // ← Đổi số này để thay đổi tần suất
  .pipe(startWith(0))
  .subscribe(() => this.loadUnreadCount());
```

### Styling Customization

**Admin component:**
- File: `notification-management.component.css`
- Màu chủ đạo: `#4a90e2` (xanh dương)
- Đổi màu: search & replace `#4a90e2` → màu mới

**User component:**
- File: `notifications-page.component.css`
- Dark theme colors:
  - Background: `#18191a`
  - Card: `#242526`
  - Unread card: `#263951`
  - Blue accent: `#2e89ff`
- Responsive breakpoint: `768px`

---

## 🐛 Troubleshooting

### Lỗi: Cannot find table 'dbo.User'
**Nguyên nhân:** Database dùng bảng `Users` (số nhiều), không phải `User`  
**Giải pháp:** Migration script đã fix, chạy lại migration

### Lỗi: NotificationDTO không có property 'createdBy'
**Nguyên nhân:** Backend DTO không có field `createdBy`  
**Giải pháp:** Đã remove references trong HTML template

### Lỗi: Compile error - cannot use arrow function in template
**Nguyên nhân:** Angular template không hỗ trợ arrow function `n => !n.isRead`  
**Giải pháp:** Đã đổi thành method `hasUnreadNotifications()` và `hasReadNotifications()`

### Frontend không connect được Backend
**Kiểm tra:**
1. Backend có đang chạy không? (`dotnet run`)
2. Frontend `environment.ts` có đúng API URL không?
3. CORS có được config chính xác không? (check `Program.cs`)
4. Browser console có lỗi 401/403 không? (token hết hạn?)

### Notification không hiển thị cho user
**Kiểm tra:**
1. Check database: `SELECT * FROM UserNotification WHERE UserID = {your_user_id}`
2. Check API response: `GET /api/usernotification` (dùng Postman + token)
3. Browser console có lỗi không?
4. User có đang login với token hợp lệ không?

---

## 📝 Notes & Best Practices

### Security
- ✅ Admin APIs chỉ cho role Admin
- ✅ User APIs extract `userId` từ JWT token (không tin client)
- ✅ Validate input với `[Required]`, `[StringLength]` attributes
- ✅ Use parameterized queries (EF Core)

### Performance
- ✅ Pagination cho danh sách thông báo
- ✅ Index trên `CreatedAt`, `UserID`, `IsRead`
- ✅ Chỉ load unread count (không load toàn bộ list)
- ✅ Polling interval 30s (không spam API)

### UX
- ✅ Loading states cho mọi async operation
- ✅ Error messages rõ ràng
- ✅ Success feedback khi thao tác thành công
- ✅ Confirm modal khi xóa
- ✅ Empty states khi không có data
- ✅ Responsive design (mobile-friendly)

### Maintainability
- ✅ Separation of concerns (Repository → Service → Controller)
- ✅ DTOs riêng cho mỗi operation
- ✅ Interface abstraction
- ✅ Standalone Angular components
- ✅ Idempotent migration script
- ✅ Comprehensive documentation

---

## 🚀 Tính năng mở rộng (Future Enhancements)

### 1. Notification Types
- Thêm column `Type` (info, warning, error, success)
- Icon và màu sắc khác nhau theo type

### 2. Rich Content
- Hỗ trợ HTML trong content
- Đính kèm hình ảnh
- Link đến trang cụ thể

### 3. Targeting
- Gửi đến group users cụ thể (role, plan...)
- Gửi đến user cụ thể (bằng UserID)

### 4. Scheduling
- Đặt lịch gửi thông báo (ScheduledTime)
- Tự động gửi vào thời điểm đã định

### 5. Push Notifications
- Tích hợp FCM (Firebase Cloud Messaging)
- Browser push notifications
- Email notifications

### 6. Analytics
- Track notification open rate
- Track click-through rate
- Dashboard thống kê

### 7. Templates
- Tạo template thông báo
- Placeholder variables: {userName}, {date}...

---

## ✅ Checklist hoàn thành

- [x] Backend DTOs (4 files)
- [x] Backend Repositories (4 files)
- [x] Backend Services (4 files)
- [x] Backend Controllers (2 files)
- [x] Database Migration script
- [x] Program.cs DI registration
- [x] Frontend Admin Service
- [x] Frontend Admin Component (TS, HTML, CSS)
- [x] Frontend User Component (TS, HTML, CSS)
- [x] Admin routing configuration
- [x] User routing configuration
- [x] Fix all TypeScript compilation errors
- [x] Fix all HTML template errors
- [x] API documentation
- [x] Implementation summary
- [x] Complete user guide

---

## 📞 Support

Nếu gặp vấn đề, kiểm tra:
1. Backend logs: `lumina_backend/lumina/bin/Debug/net8.0/`
2. Frontend console: F12 → Console tab
3. Database: Query `Notification` và `UserNotification` tables
4. API với Postman/Thunder Client để isolate issue

**Happy coding! 🎉**

# Hệ Thống Thông Báo - Lumina TOEIC

## Tổng quan
Đã hoàn thành xây dựng hệ thống thông báo toàn hệ thống với các tính năng:

### ✅ Tính năng đã hoàn thành:

#### 1. **Admin Management (CRUD Notifications)**
- ✅ Tạo thông báo mới (tự động gửi đến tất cả users)
- ✅ Xem danh sách thông báo (có phân trang)
- ✅ Xem chi tiết thông báo
- ✅ Cập nhật thông báo
- ✅ Xóa thông báo (cascade delete UserNotifications)

#### 2. **User Features**
- ✅ Xem danh sách thông báo của mình
- ✅ Xem chi tiết thông báo
- ✅ Đánh dấu đã đọc
- ✅ Đếm số thông báo chưa đọc
- ✅ Hiển thị badge thông báo chưa đọc

#### 3. **Security & Authorization**
- ✅ Admin APIs yêu cầu role "Admin"
- ✅ User APIs yêu cầu authentication
- ✅ Users chỉ xem được thông báo của mình
- ✅ JWT-based authentication

---

## Cấu trúc Files đã tạo

### Models & DTOs
```
DataLayer/
  Models/
    ✅ Notification.cs (đã cập nhật: thêm CreatedAt, CreatedBy)
    ✅ UserNotification.cs (đã có sẵn)
  
  DTOs/
    Notification/
      ✅ NotificationDTO.cs
      ✅ CreateNotificationDTO.cs
      ✅ UpdateNotificationDTO.cs
      ✅ UserNotificationDTO.cs
```

### Repository Layer
```
RepositoryLayer/
  Notification/
    ✅ INotificationRepository.cs
    ✅ NotificationRepository.cs
    ✅ IUserNotificationRepository.cs
    ✅ UserNotificationRepository.cs
```

### Service Layer
```
ServiceLayer/
  Notification/
    ✅ INotificationService.cs
    ✅ NotificationService.cs
    ✅ IUserNotificationService.cs
    ✅ UserNotificationService.cs
```

### Controllers
```
lumina/
  Controllers/
    ✅ NotificationController.cs (Admin CRUD - /api/admin/notification)
    ✅ UserNotificationController.cs (User APIs - /api/usernotification)
```

### Database Migration
```
Migrations/
  ✅ Migration_2025_01_25_NotificationSystem.sql
```

### Documentation
```
✅ NOTIFICATION_SYSTEM_API.md (Full API documentation)
✅ NOTIFICATION_IMPLEMENTATION_SUMMARY.md (This file)
```

---

## Các bước triển khai

### 1. Chạy Migration
```sql
-- Chạy file này trên SQL Server
Migrations/Migration_2025_01_25_NotificationSystem.sql
```

Migration này sẽ:
- Thêm cột `CreatedAt` vào bảng Notification
- Thêm cột `CreatedBy` vào bảng Notification (FK đến User)
- Tạo index cho performance
- Tạo FK constraint

### 2. Dependency Injection đã được cấu hình
File `Program.cs` đã được cập nhật với:
```csharp
// Notification Services
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserNotificationService, UserNotificationService>();
```

### 3. Build và Run
```bash
cd lumina
dotnet build
dotnet run
```

---

## API Endpoints

### Admin APIs (Yêu cầu role Admin)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/admin/notification?page=1&pageSize=10` | Lấy danh sách thông báo |
| GET | `/api/admin/notification/{id}` | Lấy chi tiết thông báo |
| POST | `/api/admin/notification` | Tạo thông báo mới |
| PUT | `/api/admin/notification/{id}` | Cập nhật thông báo |
| DELETE | `/api/admin/notification/{id}` | Xóa thông báo |

### User APIs (Yêu cầu authenticated)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/usernotification` | Lấy thông báo của tôi |
| GET | `/api/usernotification/unread-count` | Đếm thông báo chưa đọc |
| GET | `/api/usernotification/{uniqueId}` | Chi tiết thông báo |
| PUT | `/api/usernotification/{uniqueId}/read` | Đánh dấu đã đọc |

Chi tiết đầy đủ xem tại: **NOTIFICATION_SYSTEM_API.md**

---

## Flow hoạt động

### Admin tạo thông báo:
1. Admin đăng nhập với role "Admin"
2. POST `/api/admin/notification` với title và content
3. Hệ thống tự động:
   - Tạo Notification record
   - Lấy tất cả users có `IsActive = true`
   - Tạo UserNotification cho từng user với `IsRead = false`

### User nhận và đọc thông báo:
1. User vào app → Gọi `/api/usernotification/unread-count`
2. Hiển thị badge với số thông báo chưa đọc
3. User click icon thông báo → Gọi `/api/usernotification`
4. Hiển thị danh sách thông báo
5. User click một thông báo → Gọi `PUT /api/usernotification/{id}/read`
6. Badge tự động cập nhật

---

## Database Schema

### Notification
```sql
NotificationId INT PRIMARY KEY IDENTITY
Title NVARCHAR(200) NOT NULL
Content NVARCHAR(2000) NOT NULL
CreatedAt DATETIME2 NOT NULL
CreatedBy INT NOT NULL (FK -> User.UserId)
```

### UserNotification
```sql
UniqueId INT PRIMARY KEY IDENTITY
UserId INT NOT NULL (FK -> User.UserId)
NotificationId INT NULL (FK -> Notification.NotificationId)
IsRead BIT NULL (default = false)
CreateAt DATETIME2 NOT NULL
```

---

## Testing

### Test với Postman/Swagger:

#### 1. Admin tạo thông báo
```http
POST /api/admin/notification
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "title": "Thông báo bảo trì hệ thống",
  "content": "Hệ thống sẽ bảo trì từ 22h-24h tối nay"
}
```

#### 2. User xem thông báo
```http
GET /api/usernotification/unread-count
Authorization: Bearer {user_token}

Response: { "unreadCount": 5 }
```

```http
GET /api/usernotification
Authorization: Bearer {user_token}

Response: [array of notifications]
```

#### 3. Đánh dấu đã đọc
```http
PUT /api/usernotification/1/read
Authorization: Bearer {user_token}
```

---

## Frontend Integration

### React Example:
```javascript
// 1. Get unread count
const { data } = await axios.get('/api/usernotification/unread-count');
setBadgeCount(data.unreadCount); // Show badge

// 2. Get notifications list
const { data } = await axios.get('/api/usernotification');
setNotifications(data);

// 3. Mark as read
const handleClick = async (uniqueId) => {
  await axios.put(`/api/usernotification/${uniqueId}/read`);
  // Refresh count
};

// 4. Polling for new notifications (optional)
useEffect(() => {
  const interval = setInterval(async () => {
    const { data } = await axios.get('/api/usernotification/unread-count');
    setBadgeCount(data.unreadCount);
  }, 30000); // Every 30 seconds
  
  return () => clearInterval(interval);
}, []);
```

---

## Performance Considerations

### Đã tối ưu:
- ✅ Index trên `Notification.CreatedAt` (sắp xếp giảm dần)
- ✅ Index trên `UserNotification.CreateAt` 
- ✅ Composite index trên `UserNotification(UserId, IsRead)` để query nhanh
- ✅ Pagination trên Admin APIs
- ✅ Cascade delete khi xóa Notification

### Best Practices:
- Admin nên tạo thông báo có mục đích rõ ràng
- Không spam thông báo để tránh quá tải
- Có thể thêm scheduled job để auto-delete thông báo cũ (future enhancement)

---

## Future Enhancements (Có thể mở rộng)

### 1. Real-time Notifications với SignalR
- Push notification ngay lập tức thay vì polling
- WebSocket connection

### 2. Notification Categories
- Thêm loại thông báo: System, Promotion, Update, etc.
- Filter theo category

### 3. Scheduled Notifications
- Admin có thể schedule thông báo gửi vào thời điểm cụ thể
- Background job để gửi

### 4. Push Notifications
- Tích hợp Firebase Cloud Messaging
- Native mobile push

### 5. Notification Preferences
- User có thể tắt/bật từng loại thông báo
- Email notification option

### 6. Rich Content
- Thêm images, links
- Action buttons trong notification

---

## Troubleshooting

### Issue: "Notification not found"
- Kiểm tra NotificationId có tồn tại
- Kiểm tra quyền truy cập (Admin hoặc User)

### Issue: "Cannot create notification"
- Kiểm tra role Admin
- Kiểm tra validation (title, content không được rỗng)
- Kiểm tra CreatedBy có valid UserId

### Issue: Badge count không cập nhật
- Gọi lại API `/api/usernotification/unread-count`
- Kiểm tra `IsRead` có được set đúng

---

## Kết luận

Hệ thống thông báo đã hoàn thiện với đầy đủ tính năng:
- ✅ Admin CRUD notifications
- ✅ Tự động gửi đến tất cả users khi tạo mới
- ✅ Users xem và quản lý thông báo của mình
- ✅ Badge count cho unread notifications
- ✅ Security với role-based authorization
- ✅ Database migration đã sẵn sàng
- ✅ API documentation chi tiết

**Next Steps:**
1. Chạy migration SQL
2. Build và test APIs
3. Tích hợp frontend
4. Deploy

Nếu có thắc mắc, tham khảo file **NOTIFICATION_SYSTEM_API.md** để biết chi tiết về từng endpoint!

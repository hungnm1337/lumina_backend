# Notification System API Documentation

## Tổng quan
Hệ thống thông báo cho phép Admin tạo và quản lý thông báo hệ thống. Khi Admin tạo thông báo mới, tất cả người dùng active sẽ tự động nhận được thông báo đó.

## Database Migration
Trước khi sử dụng, chạy migration để cập nhật database:
```sql
-- File: Migrations/Migration_2025_01_25_NotificationSystem.sql
-- Chạy file này trên SQL Server để thêm các cột và index cần thiết
```

---

## Admin APIs

### 1. Lấy danh sách thông báo (Có phân trang)
**Endpoint:** `GET /api/admin/notification?page=1&pageSize=10`

**Headers:**
```json
{
  "Authorization": "Bearer {admin_token}"
}
```

**Response:**
```json
{
  "items": [
    {
      "notificationId": 1,
      "title": "Cập nhật hệ thống",
      "content": "Hệ thống sẽ bảo trì vào 20h tối nay",
      "createdAt": "2025-01-25T10:00:00Z",
      "createdBy": 1
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 10
}
```

---

### 2. Lấy chi tiết thông báo
**Endpoint:** `GET /api/admin/notification/{notificationId}`

**Headers:**
```json
{
  "Authorization": "Bearer {admin_token}"
}
```

**Response:**
```json
{
  "notificationId": 1,
  "title": "Cập nhật hệ thống",
  "content": "Hệ thống sẽ bảo trì vào 20h tối nay",
  "createdAt": "2025-01-25T10:00:00Z",
  "createdBy": 1
}
```

---

### 3. Tạo thông báo mới
**Endpoint:** `POST /api/admin/notification`

**Headers:**
```json
{
  "Authorization": "Bearer {admin_token}",
  "Content-Type": "application/json"
}
```

**Request Body:**
```json
{
  "title": "Thông báo quan trọng",
  "content": "Nội dung thông báo chi tiết..."
}
```

**Validation:**
- `title`: Bắt buộc, tối đa 200 ký tự
- `content`: Bắt buộc, tối đa 2000 ký tự

**Response:**
```json
{
  "notificationId": 5
}
```

**Note:** 
- Sau khi tạo, thông báo sẽ tự động được gửi đến **TẤT CẢ** người dùng active trong hệ thống
- Mỗi user sẽ có một bản ghi UserNotification với trạng thái `isRead = false`

---

### 4. Cập nhật thông báo
**Endpoint:** `PUT /api/admin/notification/{notificationId}`

**Headers:**
```json
{
  "Authorization": "Bearer {admin_token}",
  "Content-Type": "application/json"
}
```

**Request Body:**
```json
{
  "title": "Tiêu đề mới (optional)",
  "content": "Nội dung mới (optional)"
}
```

**Response:** `204 No Content`

---

### 5. Xóa thông báo
**Endpoint:** `DELETE /api/admin/notification/{notificationId}`

**Headers:**
```json
{
  "Authorization": "Bearer {admin_token}"
}
```

**Response:** `204 No Content`

**Note:**
- Khi xóa notification, tất cả UserNotification liên quan cũng sẽ bị xóa

---

## User APIs

### 1. Lấy danh sách thông báo của tôi
**Endpoint:** `GET /api/usernotification`

**Headers:**
```json
{
  "Authorization": "Bearer {user_token}"
}
```

**Response:**
```json
[
  {
    "uniqueId": 1,
    "userId": 10,
    "notificationId": 5,
    "title": "Thông báo quan trọng",
    "content": "Nội dung thông báo chi tiết...",
    "isRead": false,
    "createdAt": "2025-01-25T10:00:00Z"
  },
  {
    "uniqueId": 2,
    "userId": 10,
    "notificationId": 4,
    "title": "Cập nhật tính năng mới",
    "content": "Chúng tôi đã thêm tính năng...",
    "isRead": true,
    "createdAt": "2025-01-24T09:00:00Z"
  }
]
```

---

### 2. Đếm số thông báo chưa đọc
**Endpoint:** `GET /api/usernotification/unread-count`

**Headers:**
```json
{
  "Authorization": "Bearer {user_token}"
}
```

**Response:**
```json
{
  "unreadCount": 5
}
```

**Use case:**
- Hiển thị badge số lượng thông báo chưa đọc trên icon notification
- Gọi API này định kỳ hoặc khi user vào trang

---

### 3. Lấy chi tiết một thông báo
**Endpoint:** `GET /api/usernotification/{uniqueId}`

**Headers:**
```json
{
  "Authorization": "Bearer {user_token}"
}
```

**Response:**
```json
{
  "uniqueId": 1,
  "userId": 10,
  "notificationId": 5,
  "title": "Thông báo quan trọng",
  "content": "Nội dung thông báo chi tiết...",
  "isRead": false,
  "createdAt": "2025-01-25T10:00:00Z"
}
```

---

### 4. Đánh dấu đã đọc
**Endpoint:** `PUT /api/usernotification/{uniqueId}/read`

**Headers:**
```json
{
  "Authorization": "Bearer {user_token}"
}
```

**Response:** `204 No Content`

**Use case:**
- Khi user click vào thông báo để xem chi tiết, gọi API này để đánh dấu đã đọc
- Sau đó số unread count sẽ giảm đi

---

## Flow hoạt động

### Flow Admin tạo thông báo:
1. Admin đăng nhập với role "Admin"
2. Admin gọi `POST /api/admin/notification` với title và content
3. Hệ thống:
   - Tạo record Notification mới
   - Lấy danh sách tất cả UserId có `IsActive = true`
   - Tạo UserNotification cho mỗi user với `IsRead = false`
4. Trả về notificationId cho Admin

### Flow User xem thông báo:
1. User đăng nhập
2. Gọi `GET /api/usernotification/unread-count` → Hiển thị badge số thông báo chưa đọc
3. User click vào icon thông báo
4. Gọi `GET /api/usernotification` → Hiển thị danh sách thông báo
5. User click vào một thông báo cụ thể
6. Gọi `PUT /api/usernotification/{uniqueId}/read` → Đánh dấu đã đọc
7. Hiển thị chi tiết thông báo
8. Badge số thông báo chưa đọc tự động giảm

---

## Error Responses

### 401 Unauthorized
```json
{
  "message": "Missing or invalid token"
}
```

### 403 Forbidden
```json
{
  "message": "You do not have permission to access this resource"
}
```

### 404 Not Found
```json
{
  "message": "Notification not found"
}
```

### 400 Bad Request
```json
{
  "errors": {
    "Title": ["Title is required"],
    "Content": ["Content cannot exceed 2000 characters"]
  }
}
```

---

## Security

### Role-based Authorization:
- **Admin APIs** (`/api/admin/notification`): Yêu cầu role = "Admin"
- **User APIs** (`/api/usernotification`): Yêu cầu authenticated user
- User chỉ có thể xem và đánh dấu đọc thông báo của chính mình

### JWT Claims:
API sử dụng `ClaimTypes.NameIdentifier` để xác định UserId từ JWT token

---

## Database Schema

### Notification Table
```sql
- NotificationId (int, PK)
- Title (nvarchar(200), NOT NULL)
- Content (nvarchar(2000), NOT NULL)
- CreatedAt (datetime2, NOT NULL)
- CreatedBy (int, FK -> User.UserId)
```

### UserNotification Table
```sql
- UniqueId (int, PK)
- UserId (int, FK -> User.UserId)
- NotificationId (int, FK -> Notification.NotificationId)
- IsRead (bit, NULL, default = false)
- CreateAt (datetime2, NOT NULL)
```

---

## Frontend Integration Tips

### 1. Notification Bell Component
```javascript
// Hiển thị số badge
const { data } = await axios.get('/api/usernotification/unread-count');
setBadgeCount(data.unreadCount);
```

### 2. Notification List
```javascript
// Lấy danh sách thông báo
const { data } = await axios.get('/api/usernotification');
setNotifications(data);
```

### 3. Mark as Read
```javascript
// Khi user click vào thông báo
const handleNotificationClick = async (uniqueId) => {
  await axios.put(`/api/usernotification/${uniqueId}/read`);
  // Refresh unread count
  const { data } = await axios.get('/api/usernotification/unread-count');
  setBadgeCount(data.unreadCount);
};
```

### 4. Polling for New Notifications
```javascript
// Polling mỗi 30 giây để kiểm tra thông báo mới
useEffect(() => {
  const interval = setInterval(async () => {
    const { data } = await axios.get('/api/usernotification/unread-count');
    setBadgeCount(data.unreadCount);
  }, 30000);
  
  return () => clearInterval(interval);
}, []);
```

---

## Testing

### Test Admin Flow:
```bash
# 1. Login as Admin
POST /api/auth/login
{
  "email": "admin@example.com",
  "password": "password"
}

# 2. Create Notification
POST /api/admin/notification
Headers: Authorization: Bearer {admin_token}
{
  "title": "Test Notification",
  "content": "This is a test notification"
}

# 3. Get All Notifications
GET /api/admin/notification?page=1&pageSize=10
Headers: Authorization: Bearer {admin_token}
```

### Test User Flow:
```bash
# 1. Login as User
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "password"
}

# 2. Check Unread Count
GET /api/usernotification/unread-count
Headers: Authorization: Bearer {user_token}

# 3. Get My Notifications
GET /api/usernotification
Headers: Authorization: Bearer {user_token}

# 4. Mark as Read
PUT /api/usernotification/1/read
Headers: Authorization: Bearer {user_token}
```

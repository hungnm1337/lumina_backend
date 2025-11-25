# Notification System - Flow Diagram

## 📊 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         LUMINA SYSTEM                            │
│                    Notification Architecture                     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│   ADMIN ROLE    │         │   USER ROLE     │         │    DATABASE     │
│                 │         │                 │         │                 │
│  Browser        │         │  Browser        │         │  SQL Server     │
└────────┬────────┘         └────────┬────────┘         └────────┬────────┘
         │                           │                           │
         │                           │                           │
         ▼                           ▼                           ▼
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│  Angular        │         │  Angular        │         │  Tables         │
│  Component      │         │  Component      │         │                 │
│                 │         │                 │         │ • Notification  │
│ • Notification  │         │ • Notifications │         │ • UserNotif...  │
│   Management    │         │   Page          │         │ • Users         │
└────────┬────────┘         └────────┬────────┘         └─────────────────┘
         │                           │
         │                           │
         ▼                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Frontend Services                           │
│                                                                  │
│  • AdminNotificationService     • NotificationService (User)    │
│    - getAll()                     - getMyNotifications()        │
│    - create()                     - getUnreadCount()            │
│    - update()                     - markAsRead()                │
│    - delete()                                                   │
└────────┬─────────────────────────────────────────────┬──────────┘
         │                                             │
         │  HTTP Requests (Bearer Token)               │
         │                                             │
         ▼                                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Backend API Layer                           │
│                                                                  │
│  ┌────────────────────┐          ┌────────────────────┐        │
│  │ Notification       │          │ UserNotification   │        │
│  │ Controller         │          │ Controller         │        │
│  │                    │          │                    │        │
│  │ [Admin Role]       │          │ [Auth]             │        │
│  │                    │          │                    │        │
│  │ • GET    /api/...  │          │ • GET    /api/...  │        │
│  │ • POST   /api/...  │          │ • GET    .../count │        │
│  │ • PUT    /api/...  │          │ • PUT    .../read  │        │
│  │ • DELETE /api/...  │          │                    │        │
│  └────────┬───────────┘          └────────┬───────────┘        │
│           │                               │                     │
└───────────┼───────────────────────────────┼─────────────────────┘
            │                               │
            ▼                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Service Layer                               │
│                                                                  │
│  ┌────────────────────┐          ┌────────────────────┐        │
│  │ Notification       │          │ UserNotification   │        │
│  │ Service            │          │ Service            │        │
│  │                    │          │                    │        │
│  │ • Business Logic   │◄─────────┤ • GetMyNotif...    │        │
│  │ • Auto Broadcast   │          │ • MarkAsRead       │        │
│  │   when create      │          │ • GetUnreadCount   │        │
│  │                    │          │                    │        │
│  └────────┬───────────┘          └────────┬───────────┘        │
│           │                               │                     │
└───────────┼───────────────────────────────┼─────────────────────┘
            │                               │
            ▼                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Repository Layer                              │
│                                                                  │
│  ┌────────────────────┐          ┌────────────────────┐        │
│  │ Notification       │          │ UserNotification   │        │
│  │ Repository         │          │ Repository         │        │
│  │                    │          │                    │        │
│  │ • CRUD Operations  │          │ • GetByUserId      │        │
│  │ • Pagination       │          │ • MarkAsRead       │        │
│  │ • GetAllUserIds    │          │ • GetUnreadCount   │        │
│  │                    │          │                    │        │
│  └────────┬───────────┘          └────────┬───────────┘        │
│           │                               │                     │
└───────────┼───────────────────────────────┼─────────────────────┘
            │                               │
            ▼                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Entity Framework Core                       │
│                                                                  │
│                    DbContext + LINQ Queries                      │
└────────┬────────────────────────────────────────────┬───────────┘
         │                                            │
         ▼                                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SQL Server Database                         │
│                                                                  │
│  ┌─────────────────┐                ┌─────────────────┐        │
│  │  Notification   │                │ UserNotification│        │
│  ├─────────────────┤                ├─────────────────┤        │
│  │ NotificationID  │◄───────FK──────┤ UniqueID        │        │
│  │ Title           │                │ UserID (FK)     │        │
│  │ Content         │                │ NotificationID  │        │
│  │ IsActive        │                │ IsRead          │        │
│  │ CreatedAt       │                │ CreatedAt       │        │
│  │ UpdatedAt       │                │                 │        │
│  └─────────────────┘                └─────────────────┘        │
│                                                                  │
│  ┌─────────────────┐                                            │
│  │     Users       │                                            │
│  ├─────────────────┤                                            │
│  │ UserID          │◄───────FK──────┐                          │
│  │ Username        │                │                          │
│  │ Email           │                │                          │
│  │ Role            │                │                          │
│  └─────────────────┘                │                          │
│                                     │                          │
└─────────────────────────────────────┼──────────────────────────┘
                                      │
                                      │
                      CASCADE DELETE ON BOTH FKs
```

---

## 🔄 Create Notification Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                   Admin Creates Notification                     │
└─────────────────────────────────────────────────────────────────┘

1. ADMIN ACTION
   │
   ├─► Admin clicks "Tạo mới" button
   │
   ├─► Fills form:
   │   • Title: "New Event Tomorrow"
   │   • Content: "Don't forget to attend..."
   │   • IsActive: true
   │
   └─► Clicks "Tạo mới" (Create)


2. FRONTEND
   │
   ├─► AdminNotificationService.create(dto)
   │   
   └─► POST /api/admin/notification
       Headers: { Authorization: "Bearer {admin_token}" }
       Body: { title, content, isActive }


3. BACKEND - CONTROLLER
   │
   ├─► NotificationController.Create(CreateNotificationDTO)
   │   • [Authorize(Roles = "Admin")] ✓
   │   • Validate DTO ✓
   │   
   └─► Call: notificationService.CreateAsync(dto)


4. BACKEND - SERVICE
   │
   ├─► NotificationService.CreateAsync()
   │   
   ├─► Step 1: Create Notification
   │   └─► notificationRepository.CreateAsync(notification)
   │       └─► INSERT INTO Notification (Title, Content, IsActive...)
   │           Result: NotificationID = 123
   │   
   ├─► Step 2: Get All Users
   │   └─► notificationRepository.GetAllUserIdsAsync()
   │       └─► SELECT UserID FROM Users WHERE IsActive = 1
   │           Result: [1, 2, 3, 4, 5, ..., 100]
   │   
   ├─► Step 3: Create UserNotifications (AUTO BROADCAST!)
   │   └─► foreach (userId in allUserIds)
   │       └─► userNotificationRepository.CreateAsync(...)
   │           └─► INSERT INTO UserNotification 
   │               (UserID, NotificationID, IsRead)
   │               VALUES (1, 123, 0)
   │               VALUES (2, 123, 0)
   │               VALUES (3, 123, 0)
   │               ...
   │               VALUES (100, 123, 0)
   │   
   └─► Return NotificationDTO


5. BACKEND - RESPONSE
   │
   └─► HTTP 201 Created
       Body: {
         notificationId: 123,
         title: "New Event Tomorrow",
         content: "Don't forget to attend...",
         isActive: true,
         createdAt: "2025-01-25T10:30:00Z"
       }


6. FRONTEND - UPDATE UI
   │
   ├─► Success message: "Tạo thông báo thành công"
   │
   ├─► Reload notifications list
   │
   └─► Close modal
```

---

## 👀 User Views Notification Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                   User Views Notifications                       │
└─────────────────────────────────────────────────────────────────┘

1. USER ACTION
   │
   ├─► User navigates to /notifications
   │
   └─► Component: NotificationsPageComponent


2. FRONTEND - ngOnInit()
   │
   ├─► Call: loadNotifications()
   │   └─► GET /api/usernotification
   │       Headers: { Authorization: "Bearer {user_token}" }
   │
   └─► Call: loadUnreadCount()
       └─► GET /api/usernotification/unread-count
           Headers: { Authorization: "Bearer {user_token}" }


3. BACKEND - CONTROLLER
   │
   ├─► UserNotificationController.GetMyNotifications()
   │   • [Authorize] ✓
   │   • Extract userId from JWT token
   │   • userId = User.FindFirst(ClaimTypes.NameIdentifier).Value
   │   
   └─► Call: userNotificationService.GetByUserIdAsync(userId)


4. BACKEND - SERVICE
   │
   └─► UserNotificationService.GetByUserIdAsync(userId)
       │
       └─► userNotificationRepository.GetByUserIdAsync(userId)
           └─► SELECT un.UniqueID, un.UserID, un.NotificationID,
                      n.Title, n.Content, un.IsRead, un.CreatedAt
               FROM UserNotification un
               INNER JOIN Notification n 
                 ON un.NotificationID = n.NotificationID
               WHERE un.UserID = @userId
                 AND n.IsActive = 1
               ORDER BY un.CreatedAt DESC


5. BACKEND - RESPONSE
   │
   └─► HTTP 200 OK
       Body: [
         {
           uniqueId: 456,
           userId: 2,
           notificationId: 123,
           title: "New Event Tomorrow",
           content: "Don't forget to attend...",
           isRead: false,
           createdAt: "2025-01-25T10:30:00Z"
         },
         { ... },
         { ... }
       ]


6. FRONTEND - RENDER UI
   │
   ├─► notifications = response
   │
   ├─► Filter by activeTab ('all' or 'unread')
   │   └─► filteredNotifications getter
   │
   ├─► Render sections:
   │   ├─► "Mới" section (unread notifications)
   │   │   • Blue gradient icon
   │   │   • Blue dot badge
   │   │   • Dark blue background
   │   │
   │   └─► "Trước đó" section (read notifications)
   │       • Gray icon
   │       • No badge
   │       • Gray background
   │
   └─► Display unread count badge on "Chưa đọc" tab
```

---

## ✅ Mark as Read Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                   User Marks Notification as Read                │
└─────────────────────────────────────────────────────────────────┘

1. USER ACTION
   │
   └─► User clicks on notification card


2. FRONTEND
   │
   ├─► markAsRead(notification) method
   │
   ├─► Check: if (notification.isRead) return  // Already read
   │
   └─► PUT /api/usernotification/{uniqueId}/read
       Headers: { Authorization: "Bearer {user_token}" }


3. BACKEND - CONTROLLER
   │
   ├─► UserNotificationController.MarkAsRead(uniqueId)
   │   • [Authorize] ✓
   │   • Extract userId from token
   │   
   └─► Call: userNotificationService.MarkAsReadAsync(uniqueId, userId)


4. BACKEND - SERVICE
   │
   └─► UserNotificationService.MarkAsReadAsync()
       │
       ├─► Get notification: 
       │   └─► userNotificationRepository.GetByIdAsync(uniqueId)
       │       └─► SELECT * FROM UserNotification 
       │           WHERE UniqueID = @uniqueId
       │
       ├─► Security Check:
       │   └─► if (notification.UserID != userId)
       │       throw UnauthorizedAccessException  // Prevent reading others' notifications
       │
       ├─► Update:
       │   └─► notification.IsRead = true
       │       └─► UPDATE UserNotification
       │           SET IsRead = 1
       │           WHERE UniqueID = @uniqueId
       │
       └─► Return success


5. BACKEND - RESPONSE
   │
   └─► HTTP 204 No Content


6. FRONTEND - UPDATE UI
   │
   ├─► Update local state: notification.isRead = true
   │
   ├─► Decrease unread count: unreadCount--
   │
   ├─► UI automatically updates:
   │   • Blue dot disappears
   │   • Card moves from "Mới" to "Trước đó"
   │   • Background color changes to gray
   │   • Badge count decreases
   │
   └─► No page reload needed!
```

---

## 🗑️ Delete Notification Flow (Admin)

```
┌─────────────────────────────────────────────────────────────────┐
│                   Admin Deletes Notification                     │
└─────────────────────────────────────────────────────────────────┘

1. ADMIN ACTION
   │
   ├─► Admin clicks trash icon
   │
   ├─► Confirmation modal appears
   │
   └─► Admin clicks "Xóa" (Delete)


2. FRONTEND
   │
   └─► DELETE /api/admin/notification/{id}
       Headers: { Authorization: "Bearer {admin_token}" }


3. BACKEND - CONTROLLER
   │
   ├─► NotificationController.Delete(id)
   │   • [Authorize(Roles = "Admin")] ✓
   │   
   └─► Call: notificationService.DeleteAsync(id)


4. BACKEND - SERVICE
   │
   └─► NotificationService.DeleteAsync(id)
       │
       └─► notificationRepository.DeleteAsync(id)
           └─► DELETE FROM Notification 
               WHERE NotificationID = @id


5. DATABASE CASCADE DELETE
   │
   ├─► Foreign Key: FK_UserNotification_Notification
   │   • ON DELETE CASCADE
   │
   └─► Automatic deletion:
       └─► DELETE FROM UserNotification
           WHERE NotificationID = @id
           
       Result: ALL related UserNotification records deleted!
               (Could be 1000+ records if 1000 users)


6. BACKEND - RESPONSE
   │
   └─► HTTP 204 No Content


7. FRONTEND - UPDATE UI
   │
   ├─► Success message: "Xóa thông báo thành công"
   │
   ├─► Remove from notifications array
   │
   ├─► Update pagination if needed
   │
   └─► Close modal


8. USER SIDE - AUTOMATIC UPDATE
   │
   └─► Next time user loads notifications:
       └─► Deleted notification no longer appears
           (Because UserNotification records were cascade deleted)
```

---

## 🔄 Polling Flow (Real-time-ish Updates)

```
┌─────────────────────────────────────────────────────────────────┐
│                   Automatic Notification Polling                 │
└─────────────────────────────────────────────────────────────────┘

FRONTEND - notification.service.ts

1. SERVICE INITIALIZATION
   │
   └─► constructor() {
       │
       ├─► loadUnreadCount()  // Initial load
       │
       └─► interval(30000)    // Poll every 30 seconds
           .pipe(startWith(0))
           .subscribe(() => this.loadUnreadCount())
       }


2. POLLING CYCLE (Every 30 seconds)
   │
   │   Time: 00:00 ─────► 00:30 ─────► 01:00 ─────► 01:30 ─────► ...
   │           │            │            │            │
   │           ▼            ▼            ▼            ▼
   │   GET /api/usernotification/unread-count
   │
   │   Response: { unreadCount: 3 }
   │              { unreadCount: 5 }  ◄── New notifications!
   │              { unreadCount: 5 }
   │              { unreadCount: 4 }  ◄── User read one
   │
   └─► unreadCountSubject.next(newCount)


3. OBSERVABLE UPDATES
   │
   ├─► Components subscribe to: unreadCount$
   │
   └─► UI automatically updates:
       • Badge on notification bell icon
       • Badge on "Chưa đọc" tab
       • Header showing unread count


RESULT:
─────────
User doesn't need to refresh page to see new notifications!
Updates appear within 30 seconds of admin creating notification.

CUSTOMIZE INTERVAL:
──────────────────
Change 30000 to desired milliseconds:
• 10000 = 10 seconds (more real-time, more server load)
• 60000 = 1 minute (less real-time, less server load)
• 300000 = 5 minutes (minimal real-time, minimal server load)
```

---

## 📱 Responsive Design Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                   Mobile vs Desktop Layout                       │
└─────────────────────────────────────────────────────────────────┘

DESKTOP (> 768px)
─────────────────

┌────────────────────────────────────────────────────┐
│ Header: Thông báo                                  │
├────────────────────────────────────────────────────┤
│ [Tất cả]  [Chưa đọc (3)]     [Đánh dấu tất cả]   │
├────────────────────────────────────────────────────┤
│                                                    │
│  Mới                                              │
│  ┌──────────────────────────────────────────┐    │
│  │ 🔵  New Event Tomorrow           5 phút  │ ●  │
│  │     Don't forget to attend...            │    │
│  └──────────────────────────────────────────┘    │
│  ┌──────────────────────────────────────────┐    │
│  │ 🔵  System Update                2 giờ   │ ●  │
│  │     We will perform maintenance...       │    │
│  └──────────────────────────────────────────┘    │
│                                                    │
│  Trước đó                                         │
│  ┌──────────────────────────────────────────┐    │
│  │ 🔘  Welcome!                     3 ngày  │    │
│  │     Welcome to Lumina System             │    │
│  └──────────────────────────────────────────┘    │
│                                                    │
└────────────────────────────────────────────────────┘

MOBILE (< 768px)
────────────────

┌─────────────────────────┐
│ Thông báo               │
├─────────────────────────┤
│ [Tất cả] [Chưa đọc (3)]│
├─────────────────────────┤
│ [Đánh dấu tất cả đã đọc]│
├─────────────────────────┤
│ Mới                     │
│ ┌─────────────────────┐ │
│ │ 🔵  New Event     ● │ │
│ │     Tomorrow        │ │
│ │     5 phút          │ │
│ └─────────────────────┘ │
│ ┌─────────────────────┐ │
│ │ 🔵  System        ● │ │
│ │     Update          │ │
│ │     2 giờ           │ │
│ └─────────────────────┘ │
│                         │
│ Trước đó                │
│ ┌─────────────────────┐ │
│ │ 🔘  Welcome!        │ │
│ │     3 ngày          │ │
│ └─────────────────────┘ │
└─────────────────────────┘

ADJUSTMENTS:
────────────
• Tabs stack vertically
• Button takes full width
• Smaller icon (48px vs 56px)
• Smaller font sizes
• Reduced padding
```

---

## 🔐 Security Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                   Authorization & Security                       │
└─────────────────────────────────────────────────────────────────┘

JWT TOKEN STRUCTURE
───────────────────

{
  "nameid": "123",           ← UserID (ClaimTypes.NameIdentifier)
  "email": "user@example.com",
  "role": "Admin",           ← or "User"
  "exp": 1737890400,
  "iss": "LuminaSystem",
  "aud": "LuminaSystem"
}


ADMIN ENDPOINT PROTECTION
──────────────────────────

[Authorize(Roles = "Admin")]
public class NotificationController : ControllerBase
{
    // Only users with role = "Admin" can access
}

Flow:
1. Request arrives with Bearer token
2. ASP.NET Core validates token signature
3. Extracts claims (nameid, role)
4. Checks if role == "Admin"
   ├─► Yes: Allow access ✓
   └─► No:  Return 403 Forbidden ✗


USER ENDPOINT PROTECTION
─────────────────────────

[Authorize]
public class UserNotificationController : ControllerBase
{
    public async Task<IActionResult> GetMyNotifications()
    {
        // Extract userId from token (trust token, not client)
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        // Use extracted userId for query (prevents user from accessing others' data)
        var notifications = await service.GetByUserIdAsync(userId);
        
        return Ok(notifications);
    }
}

Security guarantees:
✓ User can ONLY see their own notifications
✓ Cannot manipulate userId in request body
✓ Server trusts JWT token, not client input


FRONTEND TOKEN STORAGE
───────────────────────

localStorage.setItem('lumina_token', token)

Request interceptor:
const token = localStorage.getItem('lumina_token');
headers: { Authorization: `Bearer ${token}` }


SECURITY BEST PRACTICES IMPLEMENTED
────────────────────────────────────

✓ Role-based authorization (Admin vs User)
✓ JWT token validation on every request
✓ Extract userId from token (never trust client)
✓ Parameterized queries (EF Core)
✓ Input validation ([Required], [StringLength])
✓ HTTPS in production
✓ CORS configuration
✓ No sensitive data in client-side code
✓ Token expiration handling
✓ AuthGuard on frontend routes
```

---

**End of Flow Diagrams** 🎉

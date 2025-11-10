# 🎯 Hệ Thống Mùa Giải (Season) - Lumina TOEIC

## 📋 Tổng Quan

Chức năng **Mùa Giải (Season)** được thiết kế để tạo ra chu kỳ cạnh tranh có giới hạn thời gian, thúc đẩy động lực học tập và tương tác của người dùng thông qua yếu tố **Gamification**.

## 🎯 Mục Đích Chính

### 1. Duy Trì Sự Gắn Kết & Động Lực
- Tạo cuộc đua mới mẻ và liên tục
- Người dùng có động lực học tập thường xuyên
- Khuyến khích cạnh tranh lành mạnh

### 2. Tăng Tính Công Bằng (Fairness)
- Reset điểm số sau mỗi mùa
- Người dùng mới có cơ hội cạnh tranh sòng phẳng
- Ngăn chặn tình trạng thống trị vĩnh viễn

### 3. Cơ Hội Trao Thưởng
- Trao thưởng cho top user mỗi mùa
- Khuyến khích nâng cấp gói Pro
- Tăng tham gia tích cực

## 🔄 Cơ Chế Hoạt Động

### Chu Kỳ Thời Gian
- Mỗi mùa có thời gian xác định (VD: 1 tháng, 3 tháng)
- Có ngày bắt đầu và kết thúc rõ ràng
- Tự động kích hoạt và kết thúc

### Trạng Thái Mùa Giải
- **Upcoming**: Sắp diễn ra
- **Active**: Đang diễn ra
- **Ended**: Đã kết thúc

## 📊 Hệ Thống Tính Điểm TOEIC (0-990)

### Quy Chế Tính Điểm Mới

Điểm cơ bản **giảm dần** theo trình độ TOEIC:

| Mốc TOEIC | Trình Độ | Điểm/Câu Đúng | Time Bonus | Accuracy Bonus |
|-----------|----------|---------------|------------|----------------|
| 0-200 | **Beginner** (Bắt đầu hành trình) | 15 | 30% | 150% |
| 201-400 | **Elementary** (Đang tiến bộ) | 12 | 28% | 120% |
| 401-600 | **Intermediate** (Trung bình) | 8 | 25% | 90% |
| 601-750 | **Upper-Intermediate** (Khá tốt) | 5 | 20% | 60% |
| 751-850 | **Advanced** (Sẵn sàng thi) | 3 | 15% | 40% |
| 851-990 | **Proficient** (Xuất sắc) | 2 | 10% | 20% |

### Công Thức Tính Điểm

```
Total Score = Base Score + Time Bonus + Accuracy Bonus + Difficulty Bonus

Trong đó:
- Base Score = Correct Answers × Base Points (theo mốc TOEIC)
- Time Bonus = Base Score × Time Bonus % (nếu làm nhanh < 30 phút)
- Accuracy Bonus = Base Score × Accuracy Bonus % (nếu độ chính xác ≥ 80%)
- Difficulty Bonus = Base Score × (Difficulty Multiplier - 1)
  + Easy: 0.8x
  + Medium: 1.0x
  + Hard: 1.3x
  + Expert: 1.6x
```

### Ước Tính Điểm TOEIC

```
Estimated TOEIC = (Correct Answers / Total Questions) × 990
```

Dựa trên 10 bài luyện tập gần nhất trong mùa giải.

## 🏆 Bảng Xếp Hạng

### Thông Tin Hiển Thị
- **Rank**: Thứ hạng
- **User**: Tên và avatar
- **Score**: Điểm số tích lũy
- **Estimated TOEIC**: Điểm TOEIC ước tính (0-990)
- **TOEIC Level**: Trình độ (Beginner → Proficient)

### Tính Năng
- Xem top 100 (hoặc tùy chỉnh)
- Xem thứ hạng cá nhân
- Xem thống kê chi tiết

## 🔔 Hệ Thống Thông Báo

### Loại Thông Báo

#### 1. Thông Báo Khuyến Khích (0-600 điểm)
- "Bạn đang tiến bộ tốt!"
- "Tiếp tục nỗ lực để đạt mốc Intermediate!"

#### 2. Thông Báo Thành Tích (601-850 điểm)
- "Chúc mừng! Bạn đã đạt Upper-Intermediate!"
- "Bạn đã sẵn sàng để đi thi TOEIC!"

#### 3. Thông Báo Xuất Sắc (851-990 điểm)
- "Tuyệt vời! Bạn đã đạt trình độ Proficient!"
- "Bạn đã sẵn sàng chinh phục 990 điểm!"

### Khi Nào Gửi Thông Báo
- Sau mỗi bài luyện tập
- Khi đạt mốc điểm TOEIC mới
- Khi thứ hạng thay đổi đáng kể
- Khi mùa giải sắp kết thúc

## 📡 API Endpoints

### Quản Lý Mùa Giải

#### 1. Lấy Mùa Giải Hiện Tại
```http
GET /api/leaderboard/current
```

**Response:**
```json
{
  "leaderboardId": 1,
  "seasonName": "Spring 2025",
  "seasonNumber": 1,
  "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2025-03-31T23:59:59Z",
  "isActive": true,
  "status": "Active",
  "totalParticipants": 1250,
  "daysRemaining": 45
}
```

#### 2. Tạo Mùa Giải Mới (Admin)
```http
POST /api/leaderboard
Authorization: Bearer {token}

{
  "seasonName": "Summer 2025",
  "seasonNumber": 2,
  "startDate": "2025-04-01T00:00:00Z",
  "endDate": "2025-06-30T23:59:59Z",
  "isActive": false
}
```

#### 3. Lấy Bảng Xếp Hạng
```http
GET /api/leaderboard/{leaderboardId}/ranking?top=100
```

**Response:**
```json
[
  {
    "userId": 123,
    "fullName": "Nguyễn Văn A",
    "score": 15840,
    "rank": 1,
    "estimatedTOEICScore": 785,
    "toeicLevel": "Advanced",
    "avatarUrl": "https://..."
  }
]
```

#### 4. Lấy Thống Kê Cá Nhân
```http
GET /api/leaderboard/user/stats
Authorization: Bearer {token}
```

**Response:**
```json
{
  "userId": 123,
  "currentRank": 15,
  "currentScore": 12500,
  "estimatedTOEICScore": 685,
  "toeicLevel": "Upper-Intermediate",
  "totalAttempts": 45,
  "correctAnswers": 892,
  "accuracyRate": 0.78,
  "isReadyForTOEIC": true
}
```

#### 5. Lấy Thông Tin Tính Điểm TOEIC
```http
GET /api/leaderboard/user/toeic-calculation
Authorization: Bearer {token}
```

**Response:**
```json
{
  "userId": 123,
  "estimatedTOEICScore": 685,
  "toeicLevel": "Upper-Intermediate",
  "basePointsPerCorrect": 5,
  "timeBonus": 0.20,
  "accuracyBonus": 0.60,
  "difficultyMultiplier": 1.0,
  "totalSeasonScore": 12500
}
```

#### 6. Reset Mùa Giải (Admin)
```http
POST /api/leaderboard/{leaderboardId}/reset?archiveScores=true
Authorization: Bearer {token}
```

#### 7. Tính Lại Điểm (Admin)
```http
POST /api/leaderboard/{leaderboardId}/recalculate
Authorization: Bearer {token}
```

#### 8. Tự Động Quản Lý Mùa Giải (Admin)
```http
POST /api/leaderboard/auto-manage
Authorization: Bearer {token}
```

Endpoint này nên được gọi định kỳ bởi **Background Job** để:
- Tự động kích hoạt mùa mới khi đến ngày bắt đầu
- Tự động kết thúc mùa cũ khi hết hạn

## 🔧 Cài Đặt Background Job

### Sử dụng Hangfire (Khuyến Nghị)

```csharp
// Program.cs hoặc Startup.cs
services.AddHangfire(config => 
    config.UseSqlServerStorage(connectionString));

services.AddHangfireServer();

// Scheduled Job
RecurringJob.AddOrUpdate<ILeaderboardService>(
    "auto-manage-seasons",
    service => service.AutoManageSeasonsAsync(),
    Cron.Hourly // Chạy mỗi giờ
);
```

## 💾 Database Schema

### Bảng Leaderboard (Season)
```sql
CREATE TABLE Leaderboards (
    LeaderboardId INT PRIMARY KEY IDENTITY,
    SeasonName NVARCHAR(200),
    SeasonNumber INT NOT NULL UNIQUE,
    StartDate DATETIME2,
    EndDate DATETIME2,
    IsActive BIT DEFAULT 0,
    CreateAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdateAt DATETIME2
);
```

### Bảng UserLeaderboard (Điểm User)
```sql
CREATE TABLE UserLeaderboards (
    UserLeaderboardId INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    LeaderboardId INT NOT NULL,
    Score INT DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (LeaderboardId) REFERENCES Leaderboards(LeaderboardId),
    UNIQUE (UserId, LeaderboardId)
);
```

## 🎮 Flow Hoạt Động

### 1. Khởi Tạo Mùa Mới
```
Admin tạo Season mới 
→ Hệ thống validate (không trùng ngày, số mùa)
→ Lưu vào database
→ Chờ đến ngày bắt đầu
```

### 2. Kích Hoạt Tự Động
```
Background Job chạy mỗi giờ
→ Kiểm tra mùa có StartDate <= now
→ Set IsActive = true
→ Gửi thông báo cho users
```

### 3. User Làm Bài
```
User hoàn thành bài tập
→ Tính điểm TOEIC ước tính (10 bài gần nhất)
→ Áp dụng công thức tính điểm theo mốc TOEIC
→ Cập nhật UserLeaderboard
→ Gửi thông báo nếu đạt mốc mới
```

### 4. Xem Bảng Xếp Hạng
```
User/Guest truy cập
→ Load từ UserLeaderboards
→ Sắp xếp theo Score
→ Hiển thị với thông tin TOEIC
```

### 5. Kết Thúc Mùa
```
Background Job chạy mỗi giờ
→ Kiểm tra mùa có EndDate < now
→ Set IsActive = false
→ Archive điểm số (optional)
→ Gửi thông báo kết thúc
→ Trao thưởng top users
```

## 📈 Metrics & Analytics

### Cần Theo Dõi
- Số người tham gia mỗi mùa
- Tỷ lệ hoàn thành bài tập
- Phân bố điểm TOEIC
- Tỷ lệ retention giữa các mùa
- Top performers

### Dashboard Admin
- Tổng số mùa
- Mùa hiện tại
- Top 10 users
- Biểu đồ tăng trưởng
- Thống kê theo mốc TOEIC

## 🚀 Best Practices

### 1. Tính Điểm
- Luôn tính từ 10 bài gần nhất
- Cache estimated TOEIC để tránh tính lại
- Recalculate khi cần thiết

### 2. Performance
- Index trên (LeaderboardId, Score)
- Pagination cho ranking
- Cache bảng xếp hạng (5-10 phút)

### 3. Fairness
- Không cho phép chỉnh sửa điểm thủ công
- Log mọi thay đổi
- Detect cheating patterns

### 4. UX
- Hiển thị rõ ngày kết thúc
- Countdown timer
- Thông báo trước khi kết thúc (3 ngày, 1 ngày, 1 giờ)

## 🔐 Security

### Authorization
- Admin: CRUD mùa giải
- User: Xem ranking, stats
- Guest: Xem ranking (public)

### Validation
- Không trùng SeasonNumber
- Không trùng ngày tháng
- StartDate < EndDate
- Không xóa mùa đang active

## 🐛 Troubleshooting

### Điểm không cập nhật
1. Kiểm tra ExamAttempt.Status = "Completed"
2. Kiểm tra ngày tháng attempt nằm trong season
3. Chạy Recalculate

### Mùa không tự động kích hoạt
1. Kiểm tra Background Job có chạy không
2. Kiểm tra StartDate format
3. Kiểm tra timezone

### Ranking không đúng
1. Clear cache
2. Recalculate scores
3. Kiểm tra UserLeaderboards

## 📚 Tài Liệu Tham Khảo

- [TOEIC Scoring Guide](https://www.ets.org/toeic)
- [Gamification Best Practices](https://www.gamify.com)
- [Leaderboard Design Patterns](https://www.playfab.com)

---

**Version:** 1.0  
**Last Updated:** October 30, 2025  
**Author:** Lumina Development Team

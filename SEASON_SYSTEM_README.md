# 🎮 Hệ Thống Mùa Giải (Season/Leaderboard) - Tổng Quan

## 📦 Các File Đã Được Cập Nhật

### 1. **Data Layer (DTOs)**
- ✅ `DataLayer/DTOs/Leaderboard/LeaderboardDTO.cs`
  - Thêm `TotalParticipants`, `Status`, `DaysRemaining`
  - Thêm `EstimatedTOEICScore`, `ToeicLevel` vào `LeaderboardRankDTO`
  - Thêm các DTO mới:
    - `TOEICScoreCalculationDTO`
    - `ResetSeasonDTO`
    - `UserSeasonStatsDTO`

### 2. **Repository Layer**
- ✅ `RepositoryLayer/Leaderboard/ILeaderboardRepository.cs`
  - Thêm 7 methods mới cho Season management
- ✅ `RepositoryLayer/Leaderboard/LeaderboardRepository.cs`
  - Implement logic tính điểm TOEIC (0-990)
  - Implement auto activate/end seasons
  - Thêm helper methods cho TOEIC calculation

### 3. **Service Layer**
- ✅ `ServiceLayer/Leaderboard/ILeaderboardService.cs`
  - Thêm 5 methods mới
- ✅ `ServiceLayer/Leaderboard/LeaderboardService.cs`
  - Implement business logic cho Season

### 4. **API Controller**
- ✅ `lumina/Controllers/LeaderboardController.cs`
  - Thêm 8 endpoints mới với Swagger comments
  - Authorization với ClaimTypes

### 5. **Documentation**
- ✅ `SEASON_FEATURE_GUIDE.md` - Hướng dẫn chi tiết chức năng
- ✅ `SEASON_NOTIFICATION_INTEGRATION.md` - Tích hợp thông báo
- ✅ `Migrations/SeasonLeaderboardMigration.sql` - Migration script

## 🚀 Bắt Đầu Sử Dụng

### Bước 1: Chạy Migration SQL

```bash
# Chạy file migration
sqlcmd -S localhost -d LuminaSystem -i Migrations/SeasonLeaderboardMigration.sql
```

Hoặc mở file `SeasonLeaderboardMigration.sql` và chạy trong SQL Server Management Studio.

### Bước 2: Build Backend

```bash
cd lumina_backend/lumina
dotnet restore
dotnet build
```

### Bước 3: Chạy Backend

```bash
dotnet run
```

API sẽ chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`

## 📡 API Endpoints Mới

### 🔹 Season Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/leaderboard/current` | Lấy mùa giải hiện tại | Public |
| GET | `/api/leaderboard/{id}/ranking` | Lấy bảng xếp hạng | Public |
| POST | `/api/leaderboard` | Tạo mùa mới | Admin |
| PUT | `/api/leaderboard/{id}` | Cập nhật mùa | Admin |
| DELETE | `/api/leaderboard/{id}` | Xóa mùa | Admin |
| POST | `/api/leaderboard/{id}/set-current` | Đặt mùa hiện tại | Admin |
| POST | `/api/leaderboard/{id}/recalculate` | Tính lại điểm | Admin |
| POST | `/api/leaderboard/{id}/reset` | Reset mùa | Admin |

### 🔹 User Statistics

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/leaderboard/user/stats` | Thống kê cá nhân | User |
| GET | `/api/leaderboard/user/{id}/stats` | Thống kê user khác | Public |
| GET | `/api/leaderboard/user/toeic-calculation` | Tính toán TOEIC | User |
| GET | `/api/leaderboard/user/rank` | Thứ hạng hiện tại | User |

### 🔹 Auto Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/leaderboard/auto-manage` | Tự động quản lý mùa | Admin |

## 🧪 Test API với curl

### 1. Lấy Season hiện tại
```bash
curl -X GET "http://localhost:5000/api/leaderboard/current"
```

### 2. Lấy Top 10
```bash
curl -X GET "http://localhost:5000/api/leaderboard/1/ranking?top=10"
```

### 3. Tạo Season mới (Admin)
```bash
curl -X POST "http://localhost:5000/api/leaderboard" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "seasonName": "Winter Challenge 2025",
    "seasonNumber": 4,
    "startDate": "2025-10-01T00:00:00Z",
    "endDate": "2025-12-31T23:59:59Z",
    "isActive": false
  }'
```

### 4. Lấy Stats cá nhân (User)
```bash
curl -X GET "http://localhost:5000/api/leaderboard/user/stats" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 5. Tính lại điểm (Admin)
```bash
curl -X POST "http://localhost:5000/api/leaderboard/1/recalculate" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## 📊 Quy Chế Tính Điểm TOEIC

### Mốc Điểm và Trình Độ

| TOEIC Score | Level | Điểm/Câu | Time Bonus | Accuracy Bonus |
|-------------|-------|----------|------------|----------------|
| 0-200 | Beginner | 15 | 30% | 150% |
| 201-400 | Elementary | 12 | 28% | 120% |
| 401-600 | Intermediate | 8 | 25% | 90% |
| 601-750 | Upper-Intermediate | 5 | 20% | 60% |
| 751-850 | Advanced | 3 | 15% | 40% |
| 851-990 | Proficient | 2 | 10% | 20% |

### Công Thức Tính

```
Total Score = Base Score + Time Bonus + Accuracy Bonus + Difficulty Bonus

- Base Score = Correct Answers × Base Points
- Time Bonus = Base Score × Time Bonus % (if < 30 min)
- Accuracy Bonus = Base Score × Accuracy Bonus % (if ≥ 80%)
- Difficulty Bonus = Base Score × (Difficulty - 1)
```

### Ước Tính TOEIC

```
Estimated TOEIC = (Correct / Total) × 990
```

Dựa trên **10 bài gần nhất** trong mùa giải.

## 🔔 Hệ Thống Thông Báo

Xem chi tiết tại: [`SEASON_NOTIFICATION_INTEGRATION.md`](./SEASON_NOTIFICATION_INTEGRATION.md)

### Các Loại Thông Báo

1. **Progress Notifications** - Khi đạt mốc TOEIC mới
2. **Ranking Notifications** - Khi thứ hạng thay đổi
3. **Season Notifications** - Bắt đầu/kết thúc mùa
4. **Reward Notifications** - Nhận thưởng

## 🔧 Cài Đặt Background Job (Hangfire)

### 1. Install Hangfire

```bash
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer
```

### 2. Configure in Program.cs

```csharp
// Add Hangfire services
builder.Services.AddHangfire(config => 
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// After app.Build()
app.UseHangfireDashboard("/hangfire");

// Schedule recurring jobs
RecurringJob.AddOrUpdate<ILeaderboardService>(
    "auto-manage-seasons",
    service => service.AutoManageSeasonsAsync(),
    Cron.Hourly // Chạy mỗi giờ
);
```

### 3. Access Dashboard

Truy cập: `http://localhost:5000/hangfire`

## 📈 Dashboard & Analytics

### Metrics Quan Trọng

1. **Total Participants** - Tổng số người tham gia
2. **Average Score** - Điểm trung bình
3. **TOEIC Distribution** - Phân bố theo mốc TOEIC
4. **Retention Rate** - Tỷ lệ quay lại
5. **Engagement Rate** - Tỷ lệ tham gia

### Sample Queries

```sql
-- Top performers
SELECT TOP 10 * FROM vw_CurrentSeasonStats;

-- TOEIC distribution
SELECT 
    CASE 
        WHEN Score < 200 THEN 'Beginner'
        WHEN Score < 400 THEN 'Elementary'
        WHEN Score < 600 THEN 'Intermediate'
        WHEN Score < 750 THEN 'Upper-Intermediate'
        WHEN Score < 850 THEN 'Advanced'
        ELSE 'Proficient'
    END AS Level,
    COUNT(*) AS Count
FROM UserLeaderboards
WHERE LeaderboardId = 1
GROUP BY 
    CASE 
        WHEN Score < 200 THEN 'Beginner'
        WHEN Score < 400 THEN 'Elementary'
        WHEN Score < 600 THEN 'Intermediate'
        WHEN Score < 750 THEN 'Upper-Intermediate'
        WHEN Score < 850 THEN 'Advanced'
        ELSE 'Proficient'
    END;
```

## 🎮 Frontend Integration Example

### Angular Service

```typescript
@Injectable({ providedIn: 'root' })
export class LeaderboardService {
  private apiUrl = 'http://localhost:5000/api/leaderboard';
  
  getCurrentSeason() {
    return this.http.get<LeaderboardDTO>(`${this.apiUrl}/current`);
  }
  
  getRanking(leaderboardId: number, top: number = 100) {
    return this.http.get<LeaderboardRankDTO[]>(
      `${this.apiUrl}/${leaderboardId}/ranking?top=${top}`
    );
  }
  
  getMyStats() {
    return this.http.get<UserSeasonStatsDTO>(`${this.apiUrl}/user/stats`);
  }
  
  getMyRank() {
    return this.http.get<{rank: number}>(`${this.apiUrl}/user/rank`);
  }
}
```

### Component Example

```typescript
@Component({
  selector: 'app-leaderboard',
  template: `
    <div class="leaderboard">
      <h2>{{ season?.seasonName }}</h2>
      <p>{{ season?.daysRemaining }} ngày còn lại</p>
      
      <div class="my-stats">
        <h3>Thống kê của bạn</h3>
        <p>Hạng: #{{ myStats?.currentRank }}</p>
        <p>Điểm: {{ myStats?.currentScore }}</p>
        <p>TOEIC ước tính: {{ myStats?.estimatedTOEICScore }}</p>
        <p>Trình độ: {{ myStats?.toeicLevel }}</p>
      </div>
      
      <div class="ranking">
        <h3>Top 100</h3>
        <table>
          <tr *ngFor="let user of ranking">
            <td>{{ user.rank }}</td>
            <td>{{ user.fullName }}</td>
            <td>{{ user.score }}</td>
            <td>{{ user.estimatedTOEICScore }}</td>
          </tr>
        </table>
      </div>
    </div>
  `
})
export class LeaderboardComponent implements OnInit {
  season?: LeaderboardDTO;
  myStats?: UserSeasonStatsDTO;
  ranking: LeaderboardRankDTO[] = [];
  
  constructor(private service: LeaderboardService) {}
  
  ngOnInit() {
    this.loadData();
  }
  
  loadData() {
    this.service.getCurrentSeason().subscribe(s => {
      this.season = s;
      this.service.getRanking(s.leaderboardId).subscribe(r => {
        this.ranking = r;
      });
    });
    
    this.service.getMyStats().subscribe(s => {
      this.myStats = s;
    });
  }
}
```

## 🐛 Troubleshooting

### Vấn đề 1: Điểm không cập nhật
**Giải pháp:**
1. Check `ExamAttempt.Status = "Completed"`
2. Check `EndTime` nằm trong khoảng Season
3. Chạy `POST /api/leaderboard/{id}/recalculate`

### Vấn đề 2: Season không tự động kích hoạt
**Giải pháp:**
1. Check Hangfire đang chạy
2. Check `StartDate` format đúng
3. Check timezone settings

### Vấn đề 3: Ranking không đúng
**Giải pháp:**
1. Clear cache (nếu có)
2. Recalculate scores
3. Check `UserLeaderboards` table

## 📚 Tài Liệu Tham Khảo

- [SEASON_FEATURE_GUIDE.md](./SEASON_FEATURE_GUIDE.md) - Hướng dẫn chi tiết
- [SEASON_NOTIFICATION_INTEGRATION.md](./SEASON_NOTIFICATION_INTEGRATION.md) - Tích hợp thông báo
- [Migrations/SeasonLeaderboardMigration.sql](./Migrations/SeasonLeaderboardMigration.sql) - Database migration

## ✅ Checklist Triển Khai

- [ ] Chạy migration SQL
- [ ] Build và test backend
- [ ] Test các API endpoints
- [ ] Cài đặt Hangfire
- [ ] Configure background jobs
- [ ] Tích hợp notification service
- [ ] Implement frontend components
- [ ] Test end-to-end
- [ ] Deploy to staging
- [ ] Monitor metrics

## 🎯 Kế Hoạch Phát Triển Tiếp Theo

### Phase 2 (Tháng 11-12/2025)
- [ ] Hệ thống reward (huy hiệu, kim cương)
- [ ] Leaderboard archive (lịch sử các mùa)
- [ ] Social features (theo dõi, thách đấu)
- [ ] Advanced analytics dashboard

### Phase 3 (Q1/2026)
- [ ] AI-powered practice recommendations
- [ ] Personalized TOEIC goals
- [ ] Team competitions
- [ ] Live tournaments

## 👥 Contributors

- **Backend Team**: Leaderboard API, Database
- **Frontend Team**: UI/UX Components
- **DevOps Team**: Deployment, Monitoring

## 📞 Support

Nếu có vấn đề, vui lòng:
1. Check [Troubleshooting](#-troubleshooting) section
2. Xem logs trong Hangfire dashboard
3. Contact team qua Slack channel #lumina-support

---

**Version:** 1.0  
**Last Updated:** October 30, 2025  
**License:** Proprietary - Lumina TOEIC Platform  
**Status:** ✅ Production Ready

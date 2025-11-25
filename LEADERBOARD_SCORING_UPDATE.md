# Cập nhật Logic Tính Điểm Leaderboard

## Tổng Quan Thay Đổi

Đã cập nhật nghiệp vụ tính điểm leaderboard để phân biệt rõ giữa:
1. **Điểm tích lũy (Score)**: Tăng mỗi lần làm bài, không giới hạn
2. **Điểm TOEIC ước tính (EstimatedTOEICScore)**: Chỉ tính lần đầu tiên, max 990 điểm

---

## Chi Tiết Thay Đổi

### 1. Database Schema

#### Bảng: `UserLeaderboard`

**Thêm 2 cột mới:**

| Cột | Kiểu | Nullable | Mô tả |
|-----|------|----------|-------|
| `EstimatedTOEICScore` | INT | YES | Điểm TOEIC ước tính (0-990), chỉ tính lần đầu |
| `FirstAttemptDate` | DATETIME2(3) | YES | Thời điểm làm bài lần đầu tiên trong season |

**Constraint:**
- `CK_UserLeaderboard_EstimatedTOEICScore`: Đảm bảo giá trị 0-990 hoặc NULL

**Index:**
- `IX_UserLeaderboard_FirstAttemptDate`: Filtered index cho query nhanh hơn

### 2. Logic Tính Điểm

#### Điểm Tích Lũy (Score)
- ✅ Tính mỗi lần làm bài
- ✅ Cộng dồn không giới hạn
- ✅ Khuyến khích học viên làm bài nhiều lần

**Công thức:**
```
SeasonScore = BasePoints + TimeBonus + AccuracyBonus
```

#### Điểm TOEIC Ước Tính (EstimatedTOEICScore)
- ✅ Chỉ cập nhật khi làm **đề đó** lần đầu tiên
- ✅ Tối đa 990 điểm
- ✅ Hiển thị trên bảng xếp hạng
- ✅ Tránh gaming system bằng cách làm lại cùng 1 đề

**Cách xác định:**
- Lấy 10 lần thi gần nhất (Listening + Reading)
- Tính điểm trung bình: Listening (0-495) + Reading (0-495)
- Mapping sang 6 level: Beginner → Proficient

### 3. API Response

#### CalculateScoreResponseDTO

```csharp
public class CalculateScoreResponseDTO
{
    public int SeasonScore { get; set; }              // Điểm được cộng lần này
    public int EstimatedTOEIC { get; set; }           // Điểm TOEIC ước tính (0-990)
    public string TOEICLevel { get; set; }            // Beginner/Elementary/Intermediate/Upper-Intermediate/Advanced/Proficient
    public int BasePoints { get; set; }               // Điểm cơ bản
    public int TimeBonus { get; set; }                // Thưởng về thời gian
    public int AccuracyBonus { get; set; }            // Thưởng về độ chính xác
    public bool IsFirstAttempt { get; set; }          // True = lần đầu trong season
    public string? TOEICMessage { get; set; }         // Thông báo động viên
    public int TotalAccumulatedScore { get; set; }    // Tổng điểm tích lũy hiện tại
}
```

### 4. Thông Báo Động Viên

**Mỗi lần làm bài**, user nhận được thông báo về trình độ TOEIC hiện tại:

| Level | Thông báo |
|-------|-----------|
| **Beginner** (0-200) | 🎯 Chúc mừng! Bạn đang ở trình độ Beginner với ước tính {score} điểm TOEIC. Hãy tiếp tục luyện tập để đạt 200+ điểm! |
| **Elementary** (201-400) | 📚 Tuyệt vời! Bạn đã đạt trình độ Elementary với ước tính {score} điểm TOEIC. Mục tiêu tiếp theo: 400+ điểm! |
| **Intermediate** (401-600) | ⭐ Xuất sắc! Bạn đang ở trình độ Intermediate với ước tính {score} điểm TOEIC. Tiếp tục phấn đấu để đạt 600+ điểm! |
| **Upper-Intermediate** (601-750) | 🎓 Thật ấn tượng! Bạn đã đạt Upper-Intermediate với ước tính {score} điểm TOEIC. Chỉ còn một bước nữa đến Advanced! |
| **Advanced** (751-850) | 🏆 Rất xuất sắc! Bạn đang ở trình độ Advanced với ước tính {score} điểm TOEIC. Hãy hướng tới đỉnh cao 850+ điểm! |
| **Proficient** (851-990) | 💎 Đỉnh cao! Bạn đã đạt trình độ Proficient với ước tính {score} điểm TOEIC. Bạn đang ở top đầu người học! |

---

## Migration

**File:** `Migrations/AddTOEICTrackingColumns.sql`

**Cách chạy:**
```bash
# SQL Server Management Studio
sqlcmd -S <server> -d LuminaSystem -i AddTOEICTrackingColumns.sql

# Hoặc execute trực tiếp trong SSMS
```

---

## Ví Dụ Sử Dụng

### Lần 1 (First Attempt)
**Request:**
```json
{
  "examAttemptId": 123,
  "examPartId": 1,
  "correctAnswers": 15,
  "totalQuestions": 20,
  "timeSpentSeconds": 300,
  "expectedTimeSeconds": 600
}
```

**Response:**
```json
{
  "seasonScore": 180,
  "estimatedTOEIC": 520,
  "toeicLevel": "Intermediate",
  "basePoints": 120,
  "timeBonus": 40,
  "accuracyBonus": 20,
  "isFirstAttempt": true,
  "toeicMessage": "⭐ Xuất sắc! Bạn đang ở trình độ Intermediate với ước tính 520 điểm TOEIC...",
  "totalAccumulatedScore": 180
}
```

### Lần 2 (Làm cùng đề lần 2)
**Response:**
```json
{
  "seasonScore": 150,
  "estimatedTOEIC": 540,
  "toeicLevel": "Intermediate",
  "basePoints": 100,
  "timeBonus": 30,
  "accuracyBonus": 20,
  "isFirstAttempt": false,
  "toeicMessage": "⭐ Xuất sắc! Bạn đang ở trình độ Intermediate với ước tính 540 điểm TOEIC...",
  "totalAccumulatedScore": 330
}
```

**Lưu ý:** 
- `EstimatedTOEICScore` **trong DB** vẫn là **520** (KHÔNG cập nhật vì làm lại cùng đề)
- `estimatedTOEIC` **trong response** là **540** (tính theo 10 lần thi gần nhất)
- `toeicMessage` **luôn hiển thị**
- `Score` tăng từ 180 → **330** (cộng dồn)
- **Bảng xếp hạng**: Score = 330, TOEIC = 520 (giữ nguyên)

### Lần 3 (Làm đề KHÁC lần đầu)
**Response:**
```json
{
  "seasonScore": 200,
  "estimatedTOEIC": 560,
  "toeicLevel": "Intermediate",
  "basePoints": 140,
  "timeBonus": 40,
  "accuracyBonus": 20,
  "isFirstAttempt": true,
  "toeicMessage": "⭐ Xuất sắc! Bạn đang ở trình độ Intermediate với ước tính 560 điểm TOEIC...",
  "totalAccumulatedScore": 530
}
```

**Lưu ý:**
- `EstimatedTOEICScore` **trong DB** cập nhật lên **560** (vì làm đề mới lần đầu)
- `Score` tăng từ 330 → **530**
- **Bảng xếp hạng**: Score = 530, TOEIC = 560

---

## Files Modified

1. ✅ `DataLayer/Models/UserLeaderboard.cs` - Thêm properties
2. ✅ `DataLayer/Models/LuminaSystemContext.cs` - Cấu hình EF Core
3. ✅ `DataLayer/DTOs/Leaderboard/LeaderboardDTO.cs` - Update DTO
4. ✅ `ServiceLayer/Leaderboard/LeaderboardService.cs` - Logic mới
5. ✅ `Migrations/AddTOEICTrackingColumns.sql` - Migration script

---

## Kiểm Tra

### 1. Kiểm tra Database
```sql
SELECT 
    ul.UserID,
    ul.Score,
    ul.EstimatedTOEICScore,
    ul.FirstAttemptDate,
    u.FullName
FROM UserLeaderboard ul
JOIN Users u ON ul.UserID = u.UserID
WHERE ul.LeaderboardID = (SELECT TOP 1 LeaderboardID FROM Leaderboard WHERE IsActive = 1)
ORDER BY ul.Score DESC
```

### 2. Test API
```bash
POST /api/leaderboard/calculate-score
Content-Type: application/json

{
  "examAttemptId": 123,
  "examPartId": 1,
  "correctAnswers": 18,
  "totalQuestions": 20,
  "timeSpentSeconds": 400,
  "expectedTimeSeconds": 600
}
```

---

## Notes

- ⚠️ **Quan trọng:** Chạy migration SQL trước khi deploy code mới
- 📊 Điểm TOEIC chỉ update khi `FirstAttemptDate IS NULL`
- 🎯 Điểm tích lũy không có giới hạn trên
- 💡 Thông báo chỉ hiện lần đầu (`IsFirstAttempt = true`)

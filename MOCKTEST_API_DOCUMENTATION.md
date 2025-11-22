# Mock Test API Documentation

## Overview
Mock Test API cho phép người dùng thực hiện các bài thi thử TOEIC, submit câu trả lời, và xem kết quả chi tiết với phân tích hiệu suất.

## Architecture

### Folder Structure
```
DataLayer/
  └── DTOs/
      └── MockTest/
          └── MockTestDTO.cs          # Data Transfer Objects

RepositoryLayer/
  └── MockTest/
      ├── IMockTestRepository.cs      # Repository Interface
      └── MockTestRepository.cs       # Repository Implementation

ServiceLayer/
  └── MockTest/
      ├── IMockTestService.cs         # Service Interface
      └── MockTestService.cs          # Service Implementation

lumina/
  └── Controllers/
      └── MockTestController.cs       # API Controller
```

## API Endpoints

### 1. Start Mock Test
**POST** `/api/mocktest/start`

Tạo một attempt mới cho mock test.

**Authorization:** Required

**Request Body:**
```json
{
  "userId": 123,
  "examIds": [1, 2, 3],
  "attemptType": "mock_test",
  "startTime": "2025-11-20T10:00:00Z"
}
```

**Response:**
```json
{
  "examAttemptId": 456,
  "userId": 123,
  "startTime": "2025-11-20T10:00:00Z",
  "status": "in_progress"
}
```

### 2. Submit Part Answers
**POST** `/api/mocktest/{examAttemptId}/submit-part`

Submit câu trả lời cho một phần của bài thi.

**Authorization:** Required

**Request Body:**
```json
{
  "examAttemptId": 456,
  "examId": 1,
  "answers": [
    {
      "questionId": 10,
      "userAnswer": "A",
      "isCorrect": null
    },
    {
      "questionId": 11,
      "userAnswer": "B",
      "isCorrect": null
    }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Answers submitted successfully"
}
```

### 3. Complete Mock Test
**POST** `/api/mocktest/{examAttemptId}/complete`

Hoàn thành bài thi mock test.

**Authorization:** Required

**Request Body:**
```json
{
  "endTime": "2025-11-20T12:00:00Z"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Mock test completed successfully"
}
```

### 4. Score Mock Test
**POST** `/api/mocktest/{examAttemptId}/score`

Tính điểm cho bài thi đã hoàn thành.

**Authorization:** Required

**Response:**
```json
{
  "success": true,
  "message": "Mock test scored successfully"
}
```

### 5. Get Mock Test Result
**GET** `/api/mocktest/{examAttemptId}/result`

Lấy kết quả chi tiết của mock test.

**Authorization:** Required

**Response:**
```json
{
  "examAttemptId": 456,
  "totalScore": 850,
  "listeningScore": 425,
  "readingScore": 425,
  "speakingLevel": "N/A",
  "writingLevel": "N/A",
  "completionTime": 120,
  "partResults": [
    {
      "examId": 1,
      "skillType": "listening",
      "score": 425,
      "correctAnswers": 80,
      "totalQuestions": 100,
      "timeSpent": 45
    }
  ],
  "analysis": {
    "strengths": ["Strong listening comprehension"],
    "weaknesses": ["Reading skills require more practice"],
    "recommendations": ["Practice listening to English podcasts"],
    "percentileRank": 70
  }
}
```

### 6. Get User Mock Tests
**GET** `/api/mocktest/user/{userId}`

Lấy danh sách tất cả mock tests của user.

**Authorization:** Required

**Response:**
```json
[
  {
    "examAttemptId": 456,
    "userId": 123,
    "startTime": "2025-11-20T10:00:00Z",
    "status": "scored"
  },
  {
    "examAttemptId": 455,
    "userId": 123,
    "startTime": "2025-11-19T10:00:00Z",
    "status": "completed"
  }
]
```

## Data Models

### MockTestAttemptRequestDTO
- `UserId` (int): ID của user
- `ExamIds` (List<int>): Danh sách ID các exam
- `AttemptType` (string): Loại attempt (mặc định: "mock_test")
- `StartTime` (DateTime): Thời gian bắt đầu

### PartAnswersSubmissionDTO
- `ExamAttemptId` (int): ID của exam attempt
- `ExamId` (int): ID của exam
- `Answers` (List<PartAnswerDTO>): Danh sách câu trả lời

### PartAnswerDTO
- `QuestionId` (int): ID câu hỏi
- `UserAnswer` (string): Câu trả lời của user
- `IsCorrect` (bool?): Kết quả đúng/sai (optional)

### MockTestResultDTO
- `ExamAttemptId` (int): ID của exam attempt
- `TotalScore` (int): Tổng điểm (0-990)
- `ListeningScore` (int): Điểm listening (0-495)
- `ReadingScore` (int): Điểm reading (0-495)
- `SpeakingLevel` (string): Level speaking
- `WritingLevel` (string): Level writing
- `CompletionTime` (int): Thời gian hoàn thành (phút)
- `PartResults` (List<PartResultDTO>): Kết quả từng phần
- `Analysis` (PerformanceAnalysisDTO): Phân tích hiệu suất

## Business Logic

### Scoring Algorithm
- **TOEIC Total Score:** 0-990 points
- **Listening:** 0-495 points (based on correct answers percentage)
- **Reading:** 0-495 points (based on correct answers percentage)
- Score = (Correct Answers / Total Questions) × 495

### Performance Analysis
1. **Strengths:** Identified when accuracy > 70% in a skill
2. **Weaknesses:** Identified when accuracy < 50% in a skill
3. **Recommendations:** Auto-generated based on weak areas
4. **Percentile Rank:** 
   - 90% if accuracy > 80%
   - 70% if accuracy > 60%
   - 50% if accuracy > 40%
   - 30% otherwise

## Error Handling

### Common Error Responses

**400 Bad Request:**
```json
{
  "message": "Invalid user ID"
}
```

**404 Not Found:**
```json
{
  "message": "Exam attempt 456 not found"
}
```

**500 Internal Server Error:**
```json
{
  "message": "An error occurred while starting mock test",
  "details": "Error details..."
}
```

## Flow Diagram

```
1. User starts mock test → POST /api/mocktest/start
   ↓
2. Frontend loads exam questions
   ↓
3. User answers questions → POST /api/mocktest/{id}/submit-part
   ↓
4. User completes test → POST /api/mocktest/{id}/complete
   ↓
5. System scores test → POST /api/mocktest/{id}/score
   ↓
6. User views results → GET /api/mocktest/{id}/result
```

## Testing

### Using Postman/Thunder Client

1. **Start Test:**
   ```
   POST https://localhost:7189/api/mocktest/start
   Header: Authorization: Bearer {token}
   Body: { userId, examIds, attemptType, startTime }
   ```

2. **Submit Answers:**
   ```
   POST https://localhost:7189/api/mocktest/456/submit-part
   Header: Authorization: Bearer {token}
   Body: { examAttemptId, examId, answers }
   ```

3. **Complete Test:**
   ```
   POST https://localhost:7189/api/mocktest/456/complete
   Header: Authorization: Bearer {token}
   Body: { endTime }
   ```

4. **Score Test:**
   ```
   POST https://localhost:7189/api/mocktest/456/score
   Header: Authorization: Bearer {token}
   ```

5. **Get Result:**
   ```
   GET https://localhost:7189/api/mocktest/456/result
   Header: Authorization: Bearer {token}
   ```

## Notes

- Tất cả endpoints đều yêu cầu authentication
- `ExamAttemptId` được tạo tự động khi start mock test
- Answers được tự động check đúng/sai khi submit
- Score được tính dựa trên số câu đúng/tổng số câu
- Performance analysis được generate tự động dựa trên kết quả

## Future Improvements

1. Add support for Speaking và Writing scoring
2. Implement time tracking per part
3. Add detailed question-level analytics
4. Support multiple attempt types (practice, full test, etc.)
5. Add caching for frequently accessed results
6. Implement advanced scoring algorithms matching official TOEIC

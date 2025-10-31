using Microsoft.AspNetCore.Http;

namespace DataLayer.DTOs.Exam.Speaking
{
    public class SubmitSpeakingAnswerRequest
    {
        public IFormFile Audio { get; set; }
        public int QuestionId { get; set; }
        public int AttemptId { get; set; } // ✅ THÊM: ID của lượt thi hiện tại
    }
}
using Microsoft.AspNetCore.Http;

namespace DataLayer.DTOs.Exam
{
    public class SubmitSpeakingAnswerRequest
    {
        public IFormFile Audio { get; set; }
        public int QuestionId { get; set; }
    }
}
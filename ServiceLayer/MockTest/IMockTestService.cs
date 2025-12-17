using DataLayer.DTOs.Exam;
using DataLayer.DTOs.MockTest;
using DataLayer.DTOs.Exam.Speaking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.MockTest
{
    public interface IMockTestService
    {
        Task<List<ExamPartDTO>> GetMocktestAsync();
        Task<MocktestFeedbackDTO> GetMocktestFeedbackAsync(int examAttemptId);
        Task<AttemptValidationResult> ValidateExamAttemptOwnershipAsync(int examAttemptId, int userId);
    }
}

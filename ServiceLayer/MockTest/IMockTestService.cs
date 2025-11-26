using DataLayer.DTOs.Exam;
using DataLayer.DTOs.MockTest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.MockTest
{
    public interface IMockTestService
    {
        Task<List<ExamPartDTO>> GetMocktestAsync();
        Task<MocktestFeedbackDTO> GetMocktestFeedbackAsync(int examAttemptId);
    }
}

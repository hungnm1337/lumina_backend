using DataLayer.DTOs.Exam.Speaking;
using DataLayer.DTOs.UserAnswer;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Listening
{
    public interface IListeningService
    {
        Task<AttemptValidationResult> ValidateAttemptAsync(int attemptId, int userId);
        
        Task<SubmitAnswerResponseDTO> SubmitAnswerAsync(SubmitAnswerRequestDTO request);
    }
}
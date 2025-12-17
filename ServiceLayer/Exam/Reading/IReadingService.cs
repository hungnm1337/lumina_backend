using DataLayer.DTOs.Exam.Speaking;
using DataLayer.DTOs.UserAnswer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Reading
{
    public interface IReadingService
    {
        Task<AttemptValidationResult> ValidateAttemptAsync(int attemptId, int userId);
        
        Task<SubmitAnswerResponseDTO> SubmitAnswerAsync(ReadingAnswerRequestDTO request);

    }
}

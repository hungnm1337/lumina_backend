using DataLayer.DTOs.UserAnswer;
using DataLayer.DTOs.Exam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.ExamAttempt
{
    public interface IExamAttemptService
    {
        public Task<List<ExamAttemptResponseDTO>> GetAllExamAttempts(int userId);

        public Task<ExamAttemptDetailResponseDTO> GetExamAttemptById(int attemptId);

        public Task<ExamAttemptRequestDTO> StartAnExam(ExamAttemptRequestDTO model);

        public Task<ExamAttemptRequestDTO> EndAnExam(ExamAttemptRequestDTO model);

        public Task<ExamAttemptSummaryDTO> FinalizeAttemptAsync(int attemptId);

        public Task<DataLayer.DTOs.UserAnswer.SaveProgressResponseDTO> SaveProgressAsync(SaveProgressRequestDTO request);
    }
}

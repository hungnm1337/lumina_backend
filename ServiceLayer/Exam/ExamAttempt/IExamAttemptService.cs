using DataLayer.DTOs.UserAnswer;
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

        public Task<bool> SaveReadingAnswer(ReadingAnswerRequestDTO model);
        public Task<bool> SaveWritingAnswer(WritingAnswerRequestDTO model);
    }
}

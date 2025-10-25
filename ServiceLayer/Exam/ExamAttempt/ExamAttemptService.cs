using DataLayer.DTOs.UserAnswer;
using RepositoryLayer.Exam;
using RepositoryLayer.Exam.ExamAttempt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.ExamAttempt
{
    public class ExamAttemptService : IExamAttemptService
    {
        private readonly RepositoryLayer.Exam.ExamAttempt.IExamAttemptRepository _examAttemptRepository;
        public ExamAttemptService(RepositoryLayer.Exam.ExamAttempt.IExamAttemptRepository examAttemptRepository)
        {
            _examAttemptRepository = examAttemptRepository;
        }

        public async Task<ExamAttemptRequestDTO> EndAnExam(ExamAttemptRequestDTO model)
        {
            return await _examAttemptRepository.EndAnExam(model);
        }

        public async Task<List<ExamAttemptResponseDTO>> GetAllExamAttempts(int userId)
        {
            return await _examAttemptRepository.GetAllExamAttempts(userId);
        }

        public async Task<ExamAttemptDetailResponseDTO> GetExamAttemptById(int attemptId)
        {
            return await _examAttemptRepository.GetExamAttemptById(attemptId);
        }

        public async Task<ExamAttemptRequestDTO> StartAnExam(ExamAttemptRequestDTO model)
        {
            return await _examAttemptRepository.StartAnExam(model);
        }
    }
}

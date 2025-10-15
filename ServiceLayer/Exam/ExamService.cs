using DataLayer.DTOs.Exam;
using RepositoryLayer.Exam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Exam
{
    public class ExamService : IExamService
    {
        private readonly IExamRepository _examRepository;

        public ExamService(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<List<ExamDTO>> GetAllExams(string? examType = null, string? partCode = null)
        {
            return await _examRepository.GetAllExams(examType, partCode);
        }

        public async Task<ExamDTO> GetExamDetailAndExamPartByExamID(int examId)
        {
            return await _examRepository.GetExamDetailAndExamPartByExamID(examId);
        }

        public async Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId)
        {
            return await _examRepository.GetExamPartDetailAndQuestionByExamPartID(partId);
        }
    }
}

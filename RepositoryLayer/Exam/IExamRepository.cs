using DataLayer.DTOs.Exam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Exam
{
    public interface IExamRepository
    {
        public Task<List<ExamDTO>> GetAllExams(string? examType = null, string? partCode = null);
        public Task<ExamDTO> GetExamDetailAndExamPartByExamID(int examId);

        public Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId);

    }
}

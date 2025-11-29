using DataLayer.DTOs.Exam;
using DataLayer.DTOs.ExamPart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Exam
{
    public interface IExamService
    {
        public Task<List<ExamDTO>> GetAllExams(string? examType = null, string? partCode = null);
        public Task<ExamDTO> GetExamDetailAndExamPartByExamID(int examId);

        public Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId);
        Task<bool> CreateExamFormatAsync(string fromSetKey, string toSetKey, int createdBy);
        Task<List<ExamGroupBySetKeyDto>> GetExamsGroupedBySetKeyAsync();
        Task<bool> ToggleExamStatusAsync(int examId);

        Task<List<ExamCompletionStatusDTO>> GetUserExamCompletionStatusesAsync(int userId);
        Task<ExamCompletionStatusDTO> GetExamCompletionStatusAsync(int userId, int examId);
        Task<List<PartCompletionStatusDTO>> GetPartCompletionStatusAsync(int userId, int examId);
    }
}

using DataLayer.DTOs.Exam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Exam
{
    public interface IExamService
    {
        // Lấy ra danh sách các bài thi có sẵn để chọn nhưng không bao gồm thông tin của các part
        public Task<List<ExamDTO>> GetAllExams(string? examType = null, string? partCode = null);
        // Lấy ra thông tin chi tiết của một bài thi cụ thể bao gồm cả các part bên trong
        public Task<ExamDTO> GetExamDetailAndExamPartByExamID(int examId);

        // Lấy tất cả các câu hỏi và thông tin của một phần part trong bài thi
        public Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId);


        //tạo exam
        Task<bool> CreateExamFormatAsync(string fromSetKey, string toSetKey, int createdBy);
    }
}

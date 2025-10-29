using DataLayer.DTOs.Exam;
using DataLayer.DTOs.Exam.Writting;
using DataLayer.DTOs.UserAnswer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Writting
{
    public interface IWritingService
    {
        Task<bool> SaveWritingAnswer(WritingAnswerRequestDTO writingAnswerRequestDTO);

        public Task<WritingResponseDTO> GetFeedbackFromAI(WritingRequestDTO request);
    }
}

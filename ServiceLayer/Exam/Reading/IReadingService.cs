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
        Task<SubmitAnswerResponseDTO> SubmitAnswerAsync(ReadingAnswerRequestDTO request);

    }
}

using DataLayer.DTOs.UserAnswer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Exam.Writting
{
    public interface IWrittingRepository
    {
        Task<bool> SaveWritingAnswer(WritingAnswerRequestDTO writingAnswerRequestDTO);
    }
}

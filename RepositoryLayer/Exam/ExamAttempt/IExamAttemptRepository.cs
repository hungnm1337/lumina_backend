using DataLayer.DTOs.UserAnswer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Exam.ExamAttempt
{
    public interface IExamAttemptRepository
    {
        public Task<ExamAttemptDTO> StartAnExam(ExamAttemptDTO model);

        public Task<ExamAttemptDTO> EndAnExam(ExamAttemptDTO model);
    }
}

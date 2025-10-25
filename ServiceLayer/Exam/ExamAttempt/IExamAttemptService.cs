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
        public Task<ExamAttemptDTO> StartAnExam(ExamAttemptDTO model);

        public Task<ExamAttemptDTO> EndAnExam(ExamAttemptDTO model);
    }
}

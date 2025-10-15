using DataLayer.Models;
using RepositoryLayer.Generic;

namespace RepositoryLayer.Exam
{
    public class ExamAttemptRepository : Repository<ExamAttempt>, IExamAttemptRepository
    {
        public ExamAttemptRepository(LuminaSystemContext luminaSystemContext) : base(luminaSystemContext)
        {
        }
    }
}
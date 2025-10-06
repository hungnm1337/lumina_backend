using DataLayer.Models;
using RepositoryLayer.Generic;

namespace RepositoryLayer.Questions
{
    public class QuestionRepository : Repository<Question>, IQuestionRepository
    {
        public QuestionRepository(LuminaSystemContext context) : base(context)
        {
        }
    }
}
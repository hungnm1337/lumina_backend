using DataLayer.Models;
using RepositoryLayer.Generic;

namespace RepositoryLayer.User
{
    public class UserAnswerRepository : Repository<UserAnswer>, IUserAnswerRepository
    {
        public UserAnswerRepository(LuminaSystemContext context) : base(context)
        {
        }
    }
}
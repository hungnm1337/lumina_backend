using DataLayer.Models;
using RepositoryLayer.Generic;

namespace RepositoryLayer.User
{
    public class UserAnswerSpeakingRepository : Repository<UserAnswerSpeaking>, IUserAnswerSpeakingRepository
    {
        public UserAnswerSpeakingRepository(LuminaSystemContext context) : base(context)
        {
        }
    }
}

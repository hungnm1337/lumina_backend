using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using RepositoryLayer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Vocabulary
{
    public interface ISpacedRepetitionService
    {
        Task<IEnumerable<SpacedRepetitionDTO>> GetDueForReviewAsync(int userId);
        Task<IEnumerable<SpacedRepetitionDTO>> GetUserRepetitionsAsync(int userId);
        Task<ReviewVocabularyResponseDTO> ReviewVocabularyAsync(int userId, ReviewVocabularyRequestDTO request);
        Task<SpacedRepetitionDTO?> GetByUserAndListAsync(int userId, int vocabularyListId);
        Task<SpacedRepetitionDTO> CreateRepetitionAsync(int userId, int vocabularyListId);
    }
}


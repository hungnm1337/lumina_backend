using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using RepositoryLayer.UnitOfWork;

namespace ServiceLayer.Vocabulary
{
    public class VocabularyListService : IVocabularyListService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VocabularyListService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<VocabularyListDTO> CreateListAsync(VocabularyListCreateDTO dto, int creatorUserId)
        {
            var creator = await _unitOfWork.Users.GetUserByIdAsync(creatorUserId);
            if (creator == null)
            {
                throw new KeyNotFoundException("Creator user not found.");
            }

            var newList = new VocabularyList
            {
                Name = dto.Name,
                IsPublic = dto.IsPublic,
                MakeBy = creatorUserId,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                Status = "Active"
            };

            await _unitOfWork.VocabularyLists.AddAsync(newList);
            await _unitOfWork.CompleteAsync();

            return new VocabularyListDTO
            {
                VocabularyListId = newList.VocabularyListId,
                Name = newList.Name,
                IsPublic = newList.IsPublic,
                MakeByName = creator.FullName,
                CreateAt = newList.CreateAt,
                VocabularyCount = 0
            };
        }

        public async Task<IEnumerable<VocabularyListDTO>> GetListsAsync(string? searchTerm)
        {
            return await _unitOfWork.VocabularyLists.GetAllAsync(searchTerm);
        }
    }
}
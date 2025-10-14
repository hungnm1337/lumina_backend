using DataLayer.DTOs.Passage;
using DataLayer.DTOs.Questions;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Questions
{
    public interface IQuestionRepository
    {
        Task<Passage> AddPassageAsync(Passage passage);
        Task<Prompt> AddPromptAsync(Prompt prompt);
        Task<Question> AddQuestionAsync(Question question);
        Task AddOptionsAsync(IEnumerable<Option> options);

        Task<(List<PassageDto> Items, int TotalPages)> GetPassagePromptQuestionsPagedAsync(int page, int size, int? partId);

        Task<bool> EditPassageWithPromptAsync(PassageEditDto dto);

        Task<int> AddQuestionAsync(QuestionCrudDto dto);
        Task<bool> UpdateQuestionAsync(QuestionCrudDto dto);
        Task<bool> DeleteQuestionAsync(int questionId);
    }
}

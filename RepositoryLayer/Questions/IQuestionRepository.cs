using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using DataLayer.Models;
using RepositoryLayer.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Questions
{
    public interface IQuestionRepository : IRepository<Question>

    {
        Task<Prompt> AddPromptAsync(Prompt prompt);
        Task<Question> AddQuestionAsync(Question question);
        Task AddOptionsAsync(IEnumerable<Option> options);
        Task<(List<PromptDto> Items, int TotalPages)> GetPromptsPagedAsync(int page, int size, int? partId);
        Task<bool> EditPromptWithQuestionsAsync(PromptEditDto dto);
        Task<int> AddQuestionAsync(QuestionCrudDto dto);
        Task<bool> UpdateQuestionAsync(QuestionCrudDto dto);
        Task<bool> DeleteQuestionAsync(int questionId);
        Task<bool> DeletePromptAsync(int promptId);
        Task<QuestionStatisticDto> GetQuestionStatisticsAsync();
    }
}

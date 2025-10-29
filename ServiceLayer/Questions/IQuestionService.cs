using DataLayer.DTOs.Passage;
using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Questions
{
    public interface IQuestionService
    {
        Task<int> CreatePromptWithQuestionsAsync(CreatePromptWithQuestionsDTO dto);

        Task<(List<PromptDto> Items, int TotalPages)> GetPromptsPagedAsync(int page, int size, int? partId);
           
          
          /*  Task<bool> EditPassageWithPromptAsync(PassageEditDto dto);*/

        Task<bool> EditPromptWithQuestionsAsync(PromptEditDto dto);

        Task<int> AddQuestionAsync(QuestionCrudDto dto);
        Task<bool> UpdateQuestionAsync(QuestionCrudDto dto);
        Task<bool> DeleteQuestionAsync(int questionId);

        Task<QuestionStatisticDto> GetStatisticsAsync();

        Task<List<int>> SavePromptsWithQuestionsAndOptionsAsync(
    List<CreatePromptWithQuestionsDTO> promptDtos, int partId);

        Task<int> GetAvailableSlots(int partId, int requestedCount);

        Task<bool> DeletePromptAsync(int promptId);
    }
}

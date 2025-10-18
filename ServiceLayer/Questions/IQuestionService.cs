using DataLayer.DTOs.Passage;
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
       /* Task<int> CreatePassagePromptWithQuestionsAsync(CreatePromptWithQuestionsDTO dto);*/

    /*    Task<(List<PassageDto> Items, int TotalPages)> GetPassagePromptQuestionsPagedAsync(int page, int size, int? partId);
        Task<bool> EditPassageWithPromptAsync(PassageEditDto dto);*/

        Task<int> AddQuestionAsync(QuestionCrudDto dto);
        Task<bool> UpdateQuestionAsync(QuestionCrudDto dto);
        Task<bool> DeleteQuestionAsync(int questionId);

        Task<QuestionStatisticDto> GetStatisticsAsync();
    }
}

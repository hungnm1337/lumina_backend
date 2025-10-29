using DataLayer.DTOs.AIGeneratedExam;
using DataLayer.DTOs.Questions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.ExamGenerationAI
{
    public interface IExamGenerationAIService
    {
        Task<(int partNumber, int quantity, string? topic)> ParseUserRequestAsync(string userRequest);

        Task<AIGeneratedExamDTO> GenerateExamAsync(int partNumber, int quantity, string? topic);

        Task<string> GenerateResponseAsync(string prompt);


        Task<IntentResult> DetectIntentAsync(string userRequest);
        Task<string> GeneralChatAsync(string userMessage);

        string GeneratePollinationsImageUrl(string description);


       
    }
}

using DataLayer.DTOs.AIGeneratedExam;
using DataLayer.DTOs.Questions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.ExamGenerationAI.Mappers
{
    public interface IAIExamMapper
    {
        List<CreatePromptWithQuestionsDTO> MapAIGeneratedToCreatePrompts(AIGeneratedExamDTO aiExam);
    }
}

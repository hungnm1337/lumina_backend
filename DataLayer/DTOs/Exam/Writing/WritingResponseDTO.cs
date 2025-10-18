using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Exam.Writting
{
    public class WritingResponseDTO
    {
        public int TotalScore { get; set; }
        public string GrammarFeedback { get; set; }

        public string VocabularyFeedback { get; set; }

        public string RequiredWordsCheck { get; set; }

        public string ContentAccuracyFeedback { get; set; }

        public string CorreededAnswerProposal { get; set; }
    }
}

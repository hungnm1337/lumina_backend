using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class SpeakingResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int SpeakingResultId { get; set; }

        public float? PronunciationScore { get; set; }
        public float? AccuracyScore { get; set; }
        public float? FluencyScore { get; set; }
        public float? CompletenessScore { get; set; }
        public float? GrammarScore { get; set; }
        public float? VocabularyScore { get; set; }
        public float? ContentScore { get; set; }

        public int UserAnswerId { get; set; }
        public virtual UserAnswer UserAnswer { get; set; } = null!;
    }
}
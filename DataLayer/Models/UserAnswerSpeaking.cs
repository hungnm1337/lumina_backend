using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    [Table("UserAnswerSpeaking")]
    public class UserAnswerSpeaking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserAnswerSpeakingId { get; set; }

        [Required]
        public int AttemptID { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Transcript { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PronunciationScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? AccuracyScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? FluencyScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? CompletenessScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? GrammarScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? VocabularyScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ContentScore { get; set; }

        [MaxLength(500)]
        public string AudioUrl { get; set; }

        public virtual ExamAttempt ExamAttempt { get; set; } = null!;
        public virtual Question Question { get; set; } = null!;
    }
}
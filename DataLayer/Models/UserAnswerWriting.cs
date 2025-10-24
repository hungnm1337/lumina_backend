using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    [Table("UserAnswerWriting")]
    public partial class UserAnswerWriting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserAnswerWritingId { get; set; }

        [Required]
        public int AttemptID { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string UserAnswerContent { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string FeedbackFromAI { get; set; }

        public virtual ExamAttempt ExamAttempt { get; set; } = null!;
        public virtual Question Question { get; set; } = null!;
    }
}
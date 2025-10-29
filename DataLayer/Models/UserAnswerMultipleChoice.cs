using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    [Table("UserAnswerMultipleChoice")]
    public class UserAnswerMultipleChoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserAnswerID { get; set; }

        [Required]
        public int AttemptID { get; set; }

        [Required]
        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }

        public int? Score { get; set; }

        public bool IsCorrect { get; set; }

        public virtual ExamAttempt ExamAttempt { get; set; } = null!;
        public virtual Question Question { get; set; } = null!;
        public virtual Option? SelectedOption { get; set; }
    }
}
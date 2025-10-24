using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    [Table("ExamAttempts")]
    public class ExamAttempt
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttemptID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int ExamID { get; set; }

        public int? ExamPartId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int? Score { get; set; }

        public string Status { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("ExamID")]
        public virtual Exam Exam { get; set; }

        [ForeignKey("ExamPartId")]
        public virtual ExamPart ExamPart { get; set; }

        public virtual ICollection<UserAnswerMultipleChoice> UserAnswerMultipleChoices { get; set; }
        public virtual ICollection<UserAnswerSpeaking> UserAnswerSpeakings { get; set; }
        public virtual ICollection<UserAnswerWriting> UserAnswerWritings { get; set; }

        
    }
}
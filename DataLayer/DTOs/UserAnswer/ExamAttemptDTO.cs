using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserAnswer
{
    public class ExamAttemptDTO
    {
        public int AttemptID { get; set; }

        public int UserID { get; set; }

        public int ExamID { get; set; }

        public int? ExamPartId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int? Score { get; set; }

        public string Status { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Questions
{
    public class QuestionStatisticDto
    {
        public int TotalQuestions { get; set; }
        public int UsedQuestions { get; set; }
        public int UnusedQuestions { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.AIGeneratedExam
{
    public class IntentResult
    {
        public bool IsExamRequest { get; set; }
        public string Explanation { get; set; }
    }
}

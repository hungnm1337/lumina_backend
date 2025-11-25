using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.MockTest
{
    public class MocktestFeedbackDTO
    {
        public string Overview { get; set; } 
        public int ToeicScore {get; set; }            
        public List<string> Strengths { get; set; }    
        public List<string> Weaknesses { get; set; }    
        public string ActionPlan { get; set; }
    }
}

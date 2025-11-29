namespace DataLayer.DTOs.Exam.Speaking
{
    
    public class ScoringWeights
    {
        public float PronunciationWeight { get; set; }
        public float AccuracyWeight { get; set; }
        public float FluencyWeight { get; set; }
        public float GrammarWeight { get; set; }
        public float VocabularyWeight { get; set; }
        public float ContentWeight { get; set; }

       
        public float ScaleFactor { get; set; } = 1.0f;

       
        public bool IsValid()
        {
            var sum = PronunciationWeight + AccuracyWeight + FluencyWeight +
                      GrammarWeight + VocabularyWeight + ContentWeight;
            return Math.Abs(sum - 1.0f) < 0.001f;
        }

        
        public float TotalWeight => PronunciationWeight + AccuracyWeight + FluencyWeight +
                                     GrammarWeight + VocabularyWeight + ContentWeight;
    }
}

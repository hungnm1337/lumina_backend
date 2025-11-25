namespace DataLayer.DTOs.Quota
{
    public class QuotaRemainingDto
    {
        public bool IsPremium { get; set; }
        public int ReadingRemaining { get; set; }
        public int ListeningRemaining { get; set; }
        public int ReadingUsed { get; set; }
        public int ListeningUsed { get; set; }
        public int ReadingLimit { get; set; }
        public int ListeningLimit { get; set; }
    }
}

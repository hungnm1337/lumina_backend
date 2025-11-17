namespace DataLayer.DTOs.Streak
{
    /// <summary>
    /// DTO trả về thông tin tổng quan về chuỗi học tập của user
    /// </summary>
    public class StreakSummaryDTO
    {
        /// <summary>
        /// Chuỗi hiện tại (số ngày liên tiếp)
        /// </summary>
        public int CurrentStreak { get; set; }

        /// <summary>
        /// Chuỗi dài nhất từng đạt được
        /// </summary>
        public int LongestStreak { get; set; }

        /// <summary>
        /// Hôm nay đã hoàn thành hoạt động hợp lệ chưa
        /// </summary>
        public bool TodayCompleted { get; set; }

        /// <summary>
        /// Số lượng Freeze Token còn lại
        /// </summary>
        public int FreezeTokens { get; set; }

        /// <summary>
        /// Mốc (milestone) gần nhất đã đạt được
        /// </summary>
        public int? LastMilestone { get; set; }

        /// <summary>
        /// Ngày thực hiện hoạt động hợp lệ cuối cùng (local date GMT+7)
        /// </summary>
        public DateOnly? LastPracticeDate { get; set; }

        /// <summary>
        /// Mốc tiếp theo sắp đạt được
        /// </summary>
        public int? NextMilestone { get; set; }

        /// <summary>
        /// Số ngày còn lại để đạt mốc tiếp theo
        /// </summary>
        public int? DaysToNextMilestone { get; set; }
    }
}
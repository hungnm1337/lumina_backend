namespace DataLayer.DTOs.Streak
{
    /// <summary>
    /// Các loại sự kiện liên quan đến Streak
    /// </summary>
    public enum StreakEventType
    {
        /// <summary>
        /// Hoàn thành ngày học (chuỗi tăng +1)
        /// </summary>
        CompleteDay = 1,

        /// <summary>
        /// Giữ nguyên chuỗi (học lại trong cùng ngày)
        /// </summary>
        MaintainDay = 2,

        /// <summary>
        /// Chuỗi bị reset về 1 (bắt đầu chuỗi mới)
        /// </summary>
        ResetStreak = 3,

        /// <summary>
        /// Freeze token được sử dụng (tự động)
        /// </summary>
        FreezeUsed = 4,

        /// <summary>
        /// Chuỗi bị mất hoàn toàn (reset về 0)
        /// </summary>
        StreakLost = 5,

        /// <summary>
        /// Đạt mốc chuỗi (milestone)
        /// </summary>
        MilestoneReached = 6,

        /// <summary>
        /// Nhận freeze token (từ milestone/pro/etc)
        /// </summary>
        FreezeTokenAwarded = 7
    }
}
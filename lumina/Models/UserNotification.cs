using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class UserNotification
{
    public int UniqueId { get; set; }

    public int UserId { get; set; }

    public bool? IsRead { get; set; }

    public int? NotificationId { get; set; }

    public DateTime CreateAt { get; set; }

    public virtual Notification? Notification { get; set; }

    public virtual User User { get; set; } = null!;
}

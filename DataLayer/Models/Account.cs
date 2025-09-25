using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual User User { get; set; } = null!;
}

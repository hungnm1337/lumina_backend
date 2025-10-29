﻿using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class VocabularyList
{
    public int VocabularyListId { get; set; }

    public int MakeBy { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public bool? IsPublic { get; set; }

    public bool? IsDeleted { get; set; }

    public string Name { get; set; } = null!;

    public string? Status { get; set; }

    public int? UpdatedBy { get; set; }

    public string? RejectionReason { get; set; }

    public virtual User MakeByNavigation { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserSpacedRepetition> UserSpacedRepetitions { get; set; } = new List<UserSpacedRepetition>();

    public virtual ICollection<Vocabulary> Vocabularies { get; set; } = new List<Vocabulary>();
}

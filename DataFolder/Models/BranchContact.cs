using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class BranchContact
{
    public long BranchContactId { get; set; }

    public long BranchId { get; set; }

    public long ContactId { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Contact Contact { get; set; } = null!;
}

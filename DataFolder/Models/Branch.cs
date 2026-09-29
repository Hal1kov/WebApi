using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Branch
{
    public long BranchId { get; set; }

    public long OrganizationId { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<BranchContact> BranchContacts { get; set; } = new List<BranchContact>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}

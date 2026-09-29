using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Warehouse
{
    public long WarehouseId { get; set; }

    public long BranchId { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? Address { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<WarehouseContact> WarehouseContacts { get; set; } = new List<WarehouseContact>();
}

using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class StatusEmployee
{
    public long StatusEmployeeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

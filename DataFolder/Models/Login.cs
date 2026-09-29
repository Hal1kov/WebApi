using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Login
{
    public long LoginId { get; set; }

    public string Name { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class EmployeeContact
{
    public long EmployeeContactId { get; set; }

    public long EmployeeId { get; set; }

    public long ContactId { get; set; }

    public virtual Contact Contact { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}

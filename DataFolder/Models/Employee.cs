using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Employee
{
    public long EmployeeId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Patronymic { get; set; }

    public string? Gender { get; set; }

    public DateOnly? BirthDate { get; set; }

    public long WarehouseId { get; set; }

    public long? LoginId { get; set; }

    public long? RoleId { get; set; }

    public long? StatusEmployeeId { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<EmployeeContact> EmployeeContacts { get; set; } = new List<EmployeeContact>();

    public virtual Login? Login { get; set; }

    public virtual Role? Role { get; set; }

    public virtual StatusEmployee? StatusEmployee { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}

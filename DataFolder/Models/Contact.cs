using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Contact
{
    public long ContactId { get; set; }

    public long ContactTypeId { get; set; }

    public string? Name { get; set; }

    public string Value { get; set; } = null!;

    public virtual ICollection<BranchContact> BranchContacts { get; set; } = new List<BranchContact>();

    public virtual ContactType ContactType { get; set; } = null!;

    public virtual ICollection<CounterpartyContact> CounterpartyContacts { get; set; } = new List<CounterpartyContact>();

    public virtual ICollection<EmployeeContact> EmployeeContacts { get; set; } = new List<EmployeeContact>();

    public virtual ICollection<OrganizationContact> OrganizationContacts { get; set; } = new List<OrganizationContact>();

    public virtual ICollection<WarehouseContact> WarehouseContacts { get; set; } = new List<WarehouseContact>();
}

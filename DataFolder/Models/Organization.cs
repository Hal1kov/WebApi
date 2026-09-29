using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Organization
{
    public long OrganizationId { get; set; }

    public string FullName { get; set; } = null!;

    public string? ShortName { get; set; }

    public string? LegalAddress { get; set; }

    public string TaxId { get; set; } = null!;

    public string RegistrationNumber { get; set; } = null!;

    public string? Kpp { get; set; }

    public string? Okpo { get; set; }

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();

    public virtual ICollection<OrganizationContact> OrganizationContacts { get; set; } = new List<OrganizationContact>();
}

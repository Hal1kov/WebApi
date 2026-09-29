using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class OrganizationContact
{
    public long OrganizationContactId { get; set; }

    public long OrganizationId { get; set; }

    public long ContactId { get; set; }

    public virtual Contact Contact { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;
}

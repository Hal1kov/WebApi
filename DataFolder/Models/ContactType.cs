using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class ContactType
{
    public long ContactTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}

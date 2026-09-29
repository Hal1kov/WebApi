using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class DocumentStatus
{
    public long DocumentStatusId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}

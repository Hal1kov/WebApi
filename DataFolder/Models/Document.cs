using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Document
{
    public long DocumentId { get; set; }

    public long DocumentStatusId { get; set; }

    public long? OrderId { get; set; }

    public long WarehouseId { get; set; }

    public string DocumentType { get; set; } = null!;

    public long EmployeeId { get; set; }

    public DateTime DocumentDate { get; set; }

    public string DocumentNumber { get; set; } = null!;

    public virtual ICollection<DocumentItem> DocumentItems { get; set; } = new List<DocumentItem>();

    public virtual DocumentStatus DocumentStatus { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;

    public virtual Order? Order { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}

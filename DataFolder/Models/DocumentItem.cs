using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class DocumentItem
{
    public long DocumentItemId { get; set; }

    public long DocumentId { get; set; }

    public long NomenclatureId { get; set; }

    public string? SerialNumber { get; set; }

    public string? BatchNumber { get; set; }

    public decimal Quantity { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual Nomenclature Nomenclature { get; set; } = null!;
}

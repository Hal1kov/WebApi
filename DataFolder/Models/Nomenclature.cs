using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Nomenclature
{
    public long NomenclatureId { get; set; }

    public long CategoryId { get; set; }

    public long UnitId { get; set; }

    public string Name { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<DocumentItem> DocumentItems { get; set; } = new List<DocumentItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual UnitOfMeasurement Unit { get; set; } = null!;
}

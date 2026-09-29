using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class OrderItem
{
    public long OrderItemId { get; set; }

    public long OrderId { get; set; }

    public long NomenclatureId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? Price { get; set; }

    public virtual Nomenclature Nomenclature { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}

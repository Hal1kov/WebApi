using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class WarehouseContact
{
    public long WarehouseContactId { get; set; }

    public long WarehouseId { get; set; }

    public long ContactId { get; set; }

    public virtual Contact Contact { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}

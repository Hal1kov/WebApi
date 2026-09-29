using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class OrderStatus
{
    public long OrderStatusId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

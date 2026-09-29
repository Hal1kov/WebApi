using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Order
{
    public long OrderId { get; set; }

    public long? CounterpartyId { get; set; }

    public long OrderStatusId { get; set; }

    public long WarehouseId { get; set; }

    public DateTime OrderDate { get; set; }

    public Guid? PublicToken { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public long? AcceptedByCounterpartyId { get; set; }

    public virtual Counterparty? AcceptedByCounterparty { get; set; }

    public virtual Counterparty? Counterparty { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual OrderStatus OrderStatus { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}

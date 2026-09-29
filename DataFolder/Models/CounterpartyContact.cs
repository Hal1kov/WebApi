using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class CounterpartyContact
{
    public long CounterpartyContactId { get; set; }

    public long CounterpartyId { get; set; }

    public long ContactId { get; set; }

    public virtual Contact Contact { get; set; } = null!;

    public virtual Counterparty Counterparty { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class Counterparty
{
    public long CounterpartyId { get; set; }

    public string FullName { get; set; } = null!;

    public string? ShortName { get; set; }

    public string? LegalAddress { get; set; }

    public string TaxId { get; set; } = null!;

    public string RegistrationNumber { get; set; } = null!;

    public string? Kpp { get; set; }

    public string? Okpo { get; set; }

    public virtual ICollection<CounterpartyContact> CounterpartyContacts { get; set; } = new List<CounterpartyContact>();

    public virtual ICollection<Order> OrderAcceptedByCounterparties { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderCounterparties { get; set; } = new List<Order>();
}

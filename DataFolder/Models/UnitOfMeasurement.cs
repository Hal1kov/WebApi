using System;
using System.Collections.Generic;

namespace Potok.Models;

public partial class UnitOfMeasurement
{
    public long UnitId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Nomenclature> Nomenclatures { get; set; } = new List<Nomenclature>();
}

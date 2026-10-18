using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities.Pharmacy;

public class Medicine : BaseEntity
{
    public string GenericName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public MedicineForm Form { get; set; } = MedicineForm.Tablet;
    public string Unit { get; set; } = "piece";
    public int ReorderLevel { get; set; } = 50;
    public decimal DefaultVatRate { get; set; } = 0m;
    public bool IsActive { get; set; } = true;
}

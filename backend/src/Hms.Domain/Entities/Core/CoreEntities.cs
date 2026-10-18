using Hms.Domain.Common;

namespace Hms.Domain.Entities.Core;

/// <summary>
/// Hospital entity — the top-level tenant.
/// </summary>
public class Hospital : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameBn { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? BmdcOrLicenseNo { get; set; }
    public string? VatRegNo { get; set; }
    public string Timezone { get; set; } = "Asia/Dhaka";
    public string? LogoPath { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? SettingsJson { get; set; }
    
    // Navigation
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Ward> Wards { get; set; } = new List<Ward>();
}

/// <summary>
/// Department within a hospital.
/// </summary>
public class Department : HospitalScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameBn { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Hospital Hospital { get; set; } = null!;
}

/// <summary>
/// Ward within a hospital.
/// </summary>
public class Ward : HospitalScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameBn { get; set; }
    public Enums.WardType Type { get; set; }
    public int Floor { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Hospital Hospital { get; set; } = null!;
    public ICollection<Bed> Beds { get; set; } = new List<Bed>();
}

/// <summary>
/// Bed within a ward, with concurrency control for safe status updates.
/// </summary>
public class Bed : HospitalScopedConcurrentEntity
{
    public Guid WardId { get; set; }
    public string BedNo { get; set; } = string.Empty;
    public Enums.BedStatus Status { get; set; } = Enums.BedStatus.Free;

    // Navigation
    public Ward Ward { get; set; } = null!;
}

/// <summary>
/// Bangladesh administrative division (top level).
/// </summary>
public class GeoDivision
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameBn { get; set; } = string.Empty;
    
    public ICollection<GeoDistrict> Districts { get; set; } = new List<GeoDistrict>();
}

/// <summary>
/// Bangladesh administrative district.
/// </summary>
public class GeoDistrict
{
    public int Id { get; set; }
    public int DivisionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameBn { get; set; } = string.Empty;

    public GeoDivision Division { get; set; } = null!;
    public ICollection<GeoUpazila> Upazilas { get; set; } = new List<GeoUpazila>();
}

/// <summary>
/// Bangladesh administrative upazila/thana.
/// </summary>
public class GeoUpazila
{
    public int Id { get; set; }
    public int DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameBn { get; set; } = string.Empty;

    public GeoDistrict District { get; set; } = null!;
}

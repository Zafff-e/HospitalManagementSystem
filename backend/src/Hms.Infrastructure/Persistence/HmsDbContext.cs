using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Hms.Domain.Common;
using Hms.Domain.Entities.Core;
using Hms.Domain.Entities.Patient;
using Hms.Domain.Entities.Clinical;
using Hms.Domain.Entities.Audit;
using Hms.Infrastructure.Identity;

namespace Hms.Infrastructure.Persistence;

public class HmsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public HmsDbContext(DbContextOptions<HmsDbContext> options) : base(options)
    {
    }

    // Identity & Permissions
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Core
    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<GeoDivision> Divisions => Set<GeoDivision>();
    public DbSet<GeoDistrict> Districts => Set<GeoDistrict>();
    public DbSet<GeoUpazila> Upazilas => Set<GeoUpazila>();

    // Patient
    public DbSet<PatientRecord> Patients => Set<PatientRecord>();
    public DbSet<PatientEmergencyContact> PatientEmergencyContacts => Set<PatientEmergencyContact>();

    // Clinical
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<OpdToken> OpdTokens => Set<OpdToken>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<VitalSign> VitalSigns => Set<VitalSign>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();

    // Pharmacy
    public DbSet<Hms.Domain.Entities.Pharmacy.Medicine> Medicines => Set<Hms.Domain.Entities.Pharmacy.Medicine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure RolePermission composite key
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(rp => rp.RoleId);
            entity.HasOne(rp => rp.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(rp => rp.PermissionId);
        });

        // Configure RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId);
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Entity, e.EntityId });
            entity.HasIndex(e => e.CreatedAt);
        });

        // Apply global soft delete filter for BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var propertyMethodInfo = typeof(EF).GetMethod("Property")!.MakeGenericMethod(typeof(bool));
                var isDeletedProperty = System.Linq.Expressions.Expression.Call(propertyMethodInfo, parameter, System.Linq.Expressions.Expression.Constant("IsDeleted"));
                var compareExpression = System.Linq.Expressions.Expression.MakeBinary(
                    System.Linq.Expressions.ExpressionType.Equal,
                    isDeletedProperty,
                    System.Linq.Expressions.Expression.Constant(false)
                );
                var lambda = System.Linq.Expressions.Expression.Lambda(compareExpression, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Hospital configurations
        modelBuilder.Entity<Hospital>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(250).IsRequired();
            entity.Property(e => e.NameBn).HasMaxLength(250);
            entity.Property(e => e.Timezone).HasMaxLength(50).HasDefaultValue("Asia/Dhaka");
        });

        // Bed configuration: concurrency token on RowVersion
        modelBuilder.Entity<Bed>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BedNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(e => e.Ward)
                  .WithMany(w => w.Beds)
                  .HasForeignKey(e => e.WardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PatientRecord configuration
        modelBuilder.Entity<PatientRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.HospitalId, e.MRN }).IsUnique();
            entity.Property(e => e.MRN).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PhonePrimary).HasMaxLength(20).IsRequired();
            entity.Property(e => e.NidLast4).HasMaxLength(4);
            entity.Property(e => e.FirstNameEn).HasMaxLength(100);
            entity.Property(e => e.LastNameEn).HasMaxLength(100);
            entity.Property(e => e.FirstNameBn).HasMaxLength(100);
            entity.Property(e => e.LastNameBn).HasMaxLength(100);
        });

        // Doctor configuration
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BmdcRegNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.ConsultationFee).HasPrecision(18, 2);
        });

        // VitalSign precision
        modelBuilder.Entity<VitalSign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Temperature).HasPrecision(4, 1);
            entity.Property(e => e.Weight).HasPrecision(5, 2);
            entity.Property(e => e.Height).HasPrecision(5, 1);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.NewGuid();
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Soft delete
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

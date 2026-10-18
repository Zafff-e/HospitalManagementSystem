using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hms.Domain.Entities.Core;
using Hms.Infrastructure.Identity;

namespace Hms.Infrastructure.Persistence;

public static class DbSeeder
{
    public static readonly Guid DemoHospitalId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(
        HmsDbContext context, 
        UserManager<ApplicationUser> userManager, 
        RoleManager<ApplicationRole> roleManager)
    {
        // 1. Seed Hospital
        var hospital = await context.Hospitals.FirstOrDefaultAsync(h => h.Id == DemoHospitalId);
        if (hospital == null)
        {
            hospital = new Hospital
            {
                Id = DemoHospitalId,
                Name = "Sample Medical Centre, Dhaka (demo)",
                NameBn = "নমুনা মেডিকেল সেন্টার, ঢাকা (ডেমো)",
                Address = "House 12, Road 5, Dhanmondi, Dhaka-1205",
                Timezone = "Asia/Dhaka",
                BmdcOrLicenseNo = "DGHS-LIC-2026-DEMO",
                VatRegNo = "BIN-999888777-DEMO"
            };
            context.Hospitals.Add(hospital);
            await context.SaveChangesAsync();
        }

        // 2. Seed Permissions
        var permissions = new List<(string Name, string Category, string Description)>
        {
            ("patient.read", "Patient", "View patient profiles and medical records"),
            ("patient.write", "Patient", "Create and update patient records"),
            ("patient.delete", "Patient", "Soft delete patient records"),
            ("appt.read", "Appointment", "View doctor schedules and appointments"),
            ("appt.write", "Appointment", "Book appointments"),
            ("appt.checkin", "Appointment", "Check in appointments and generate OPD tokens"),
            ("emr.read", "Clinical", "View patient vitals, diagnosis, and clinical notes"),
            ("emr.write", "Clinical", "Record vitals and clinical notes"),
            ("emr.prescribe", "Clinical", "Issue medical prescriptions (requires BMDC reg)"),
            ("ipd.read", "IPD", "View ward and bed occupancy"),
            ("ipd.admit", "IPD", "Admit patient to ward/bed"),
            ("ipd.transfer", "IPD", "Transfer patient bed"),
            ("ipd.discharge", "IPD", "Discharge admitted patient"),
            ("billing.read", "Billing", "View invoices and payment receipts"),
            ("billing.create", "Billing", "Create invoices and process payments"),
            ("billing.refund", "Billing", "Approve and process payment refunds"),
            ("lab.result.enter", "Laboratory", "Enter test findings"),
            ("lab.result.verify", "Laboratory", "Verify lab test results"),
            ("pharm.dispense", "Pharmacy", "Dispense medicine against prescription"),
            ("admin.users", "Admin", "Manage users and role assignments")
        };

        var existingPermissions = await context.Permissions.ToDictionaryAsync(p => p.Name);
        foreach (var (name, category, description) in permissions)
        {
            if (!existingPermissions.ContainsKey(name))
            {
                var perm = new Permission
                {
                    Name = name,
                    Category = category,
                    Description = description
                };
                context.Permissions.Add(perm);
                existingPermissions[name] = perm;
            }
        }
        await context.SaveChangesAsync();

        // 3. Seed Roles
        var roles = new List<string>
        {
            "SuperAdmin", "Admin", "Receptionist", "Doctor", "Nurse", 
            "LabTech", "Pharmacist", "BillingOfficer", "OTCoordinator", 
            "BloodBankTech", "HRManager", "Management"
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    Description = $"{roleName} role for hospital management"
                });
            }
        }

        // 4. Map Role Permissions
        var roleMappings = new Dictionary<string, List<string>>
        {
            ["Receptionist"] = new() { "patient.read", "patient.write", "appt.read", "appt.write", "appt.checkin", "ipd.read" },
            ["BillingOfficer"] = new() { "billing.read", "billing.create", "billing.refund", "patient.read" },
            ["Doctor"] = new() { "emr.read", "emr.write", "emr.prescribe", "patient.read", "appt.read" }
        };

        foreach (var (roleName, permList) in roleMappings)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var currentRolePerms = await context.RolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                foreach (var permName in permList)
                {
                    if (existingPermissions.TryGetValue(permName, out var perm) && !currentRolePerms.Contains(perm.Id))
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = perm.Id
                        });
                    }
                }
            }
        }
        await context.SaveChangesAsync();

        // 5. Seed Demo Users
        var demoUsers = new List<(string Email, string Name, string Role, string? StaffId, string? BmdcNo)>
        {
            ("superadmin@demo-hms.local", "Dr. Super Admin", "SuperAdmin", "EMP-001", null),
            ("receptionist@demo-hms.local", "Fatima Rahman", "Receptionist", "EMP-101", null),
            ("billing@demo-hms.local", "Kamal Hossain", "BillingOfficer", "EMP-201", null),
            ("doctor@demo-hms.local", "Dr. Mahmud Hasan", "Doctor", "EMP-301", "DEMO-1001")
        };

        const string DefaultPassword = "Password123!";

        foreach (var (email, name, role, staffId, bmdcNo) in demoUsers)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = name,
                    StaffId = staffId,
                    BmdcRegNo = bmdcNo,
                    HospitalId = DemoHospitalId,
                    IsActive = true
                };

                var createResult = await userManager.CreateAsync(user, DefaultPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}

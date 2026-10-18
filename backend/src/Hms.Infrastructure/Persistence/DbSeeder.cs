using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hms.Domain.Entities.Core;
using Hms.Domain.Entities.Patient;
using Hms.Domain.Enums;
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

        // 6. Seed Bangladesh Geo Data
        await SeedGeoDataAsync(context);

        // 7. Seed 200 Fictional Patients
        await SeedPatientsAsync(context);
    }

    public static async Task SeedGeoDataAsync(HmsDbContext context)
    {
        if (await context.Divisions.AnyAsync()) return;

        var divisions = new List<GeoDivision>
        {
            new() { Id = 1, Name = "Dhaka", NameBn = "ঢাকা" },
            new() { Id = 2, Name = "Chattogram", NameBn = "চট্টগ্রাম" },
            new() { Id = 3, Name = "Rajshahi", NameBn = "রাজশাহী" },
            new() { Id = 4, Name = "Khulna", NameBn = "খুলনা" },
            new() { Id = 5, Name = "Barishal", NameBn = "বরিশাল" },
            new() { Id = 6, Name = "Sylhet", NameBn = "সিলেট" },
            new() { Id = 7, Name = "Rangpur", NameBn = "রংপুর" },
            new() { Id = 8, Name = "Mymensingh", NameBn = "ময়মনসিংহ" }
        };
        context.Divisions.AddRange(divisions);
        await context.SaveChangesAsync();

        var districts = new List<GeoDistrict>
        {
            // Dhaka Division
            new() { Id = 1, DivisionId = 1, Name = "Dhaka", NameBn = "ঢাকা" },
            new() { Id = 2, DivisionId = 1, Name = "Gazipur", NameBn = "গাজীপুর" },
            new() { Id = 3, DivisionId = 1, Name = "Narayanganj", NameBn = "নারায়ণগঞ্জ" },
            new() { Id = 4, DivisionId = 1, Name = "Tangail", NameBn = "টাঙ্গাইল" },
            // Chattogram Division
            new() { Id = 5, DivisionId = 2, Name = "Chattogram", NameBn = "চট্টগ্রাম" },
            new() { Id = 6, DivisionId = 2, Name = "Cox's Bazar", NameBn = "কক্সবাজার" },
            new() { Id = 7, DivisionId = 2, Name = "Cumilla", NameBn = "কুমিল্লা" },
            // Rajshahi Division
            new() { Id = 8, DivisionId = 3, Name = "Rajshahi", NameBn = "রাজশাহী" },
            new() { Id = 9, DivisionId = 3, Name = "Bogura", NameBn = "বগুড়া" },
            // Khulna Division
            new() { Id = 10, DivisionId = 4, Name = "Khulna", NameBn = "খুলনা" },
            new() { Id = 11, DivisionId = 4, Name = "Jashore", NameBn = "যশোর" },
            // Sylhet Division
            new() { Id = 12, DivisionId = 6, Name = "Sylhet", NameBn = "সিলেট" }
        };
        context.Districts.AddRange(districts);
        await context.SaveChangesAsync();

        var upazilas = new List<GeoUpazila>
        {
            // Dhaka District
            new() { Id = 1, DistrictId = 1, Name = "Dhanmondi", NameBn = "ধানমন্ডি" },
            new() { Id = 2, DistrictId = 1, Name = "Mirpur", NameBn = "মিরপুর" },
            new() { Id = 3, DistrictId = 1, Name = "Gulshan", NameBn = "গুলশান" },
            new() { Id = 4, DistrictId = 1, Name = "Uttara", NameBn = "উত্তরা" },
            new() { Id = 5, DistrictId = 1, Name = "Mohammadpur", NameBn = "মোহাম্মদপুর" },
            new() { Id = 6, DistrictId = 1, Name = "Savar", NameBn = "সাভার" },
            // Gazipur District
            new() { Id = 7, DistrictId = 2, Name = "Gazipur Sadar", NameBn = "গাজীপুর সদর" },
            new() { Id = 8, DistrictId = 2, Name = "Tongi", NameBn = "টঙ্গী" },
            // Narayanganj District
            new() { Id = 9, DistrictId = 3, Name = "Narayanganj Sadar", NameBn = "নারায়ণগঞ্জ সদর" },
            // Chattogram District
            new() { Id = 10, DistrictId = 5, Name = "Panchlaish", NameBn = "পাঁচলাইশ" },
            new() { Id = 11, DistrictId = 5, Name = "Kotwali", NameBn = "কোতোয়ালী" },
            // Sylhet District
            new() { Id = 12, DistrictId = 12, Name = "Sylhet Sadar", NameBn = "সিলেট সদর" }
        };
        context.Upazilas.AddRange(upazilas);
        await context.SaveChangesAsync();
    }

    public static async Task SeedPatientsAsync(HmsDbContext context)
    {
        if (await context.Patients.CountAsync() >= 200) return;

        var firstNamesEn = new[] { "Mohammad", "Fatema", "Abdul", "Nusrat", "Rashed", "Shirin", "Tanvir", "Nasrin", "Tariq", "Salma", "Imran", "Farhana", "Jashim", "Rabeya", "Kazi", "Rokeya", "Anisur", "Taslima", "Habib", "Shamima" };
        var lastNamesEn = new[] { "Rahman", "Hasan", "Ahmed", "Islam", "Chowdhury", "Khatun", "Akter", "Hossain", "Uddin", "Begum", "Siddique", "Miah", "Ali", "Bhuiyan", "Khan" };
        var firstNamesBn = new[] { "মোহাম্মদ", "ফাতেমা", "আব্দুল", "নুসরাত", "রাশেদ", "শিরিন", "তানভীর", "নাসরিন", "তারিক", "সালমা", "ইমরান", "ফারহানা", "জসিম", "রাবেয়া", "কাজী", "রোকেয়া", "আনিসুর", "তাসলিমা", "হাবিব", "শামীমা" };
        var lastNamesBn = new[] { "রহমান", "হাসান", "আহমেদ", "ইসলাম", "চৌধুরী", "খাতুন", "আক্তার", "হোসেন", "উদ্দিন", "বেগম", "সিদ্দিক", "মিয়া", "আলী", "ভূঁইয়া", "খান" };

        var bloodGroups = new[] { BloodGroup.APositive, BloodGroup.ANegative, BloodGroup.BPositive, BloodGroup.BNegative, BloodGroup.OPositive, BloodGroup.ONegative, BloodGroup.ABPositive, BloodGroup.ABNegative };
        var genders = new[] { Gender.Male, Gender.Female };

        var year = DateTime.UtcNow.Year;
        var existingCount = await context.Patients.CountAsync();
        var patientsToAdd = new List<PatientRecord>();

        for (int i = 1; i <= 200 - existingCount; i++)
        {
            var seq = existingCount + i;
            var fnIdx = (seq - 1) % firstNamesEn.Length;
            var lnIdx = (seq - 1) % lastNamesEn.Length;
            var bgIdx = (seq - 1) % bloodGroups.Length;
            var gIdx = fnIdx % 2; // Rough alternating gender

            var phoneNum = $"+88017{seq:D8}";
            var nidDigits = $"1990{seq:D6}123";
            var dob = new DateTime(1960 + (seq % 45), 1 + (seq % 12), 1 + (seq % 28));

            var patient = new PatientRecord
            {
                HospitalId = DemoHospitalId,
                MRN = $"{year}-{seq:D6}",
                FirstNameEn = firstNamesEn[fnIdx],
                LastNameEn = lastNamesEn[lnIdx],
                FirstNameBn = firstNamesBn[fnIdx],
                LastNameBn = lastNamesBn[lnIdx],
                Gender = genders[gIdx],
                BloodGroup = bloodGroups[bgIdx],
                DateOfBirth = dob,
                PhonePrimary = phoneNum,
                Email = $"patient{seq}@fictional-mail.local",
                NidEncrypted = $"ENC_AES_{nidDigits}",
                NidLast4 = nidDigits[^4..],
                DivisionId = 1,
                DistrictId = 1,
                UpazilaId = 1 + (seq % 6),
                AddressLine = $"Road {1 + (seq % 20)}, Sector {1 + (seq % 14)}",
                Occupation = seq % 3 == 0 ? "Teacher" : (seq % 3 == 1 ? "Engineer" : "Merchant"),
                MaritalStatus = seq % 2 == 0 ? MaritalStatus.Married : MaritalStatus.Single,
                Religion = "Islam"
            };

            patientsToAdd.Add(patient);
        }

        context.Patients.AddRange(patientsToAdd);
        await context.SaveChangesAsync();
    }
}

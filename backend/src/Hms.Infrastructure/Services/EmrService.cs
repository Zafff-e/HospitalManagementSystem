using Hms.Application.Common.Interfaces;
using Hms.Application.Emr.Dtos;
using Hms.Domain.Entities.Clinical;
using Hms.Domain.Entities.Core;
using Hms.Domain.Entities.Pharmacy;
using Hms.Domain.Enums;
using Hms.Infrastructure.Clinical;
using Hms.Infrastructure.Identity;
using Hms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hms.Infrastructure.Services;

public class EmrService : IEmrService
{
    private readonly HmsDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EmrService> _logger;

    static EmrService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    public EmrService(
        HmsDbContext context,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        ILogger<EmrService> logger)
    {
        _context = context;
        _auditService = auditService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<EncounterResponse> StartEncounterAsync(
        Guid hospitalId, 
        Guid doctorUserId, 
        CreateEncounterRequest request, 
        CancellationToken ct = default)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.HospitalId == hospitalId, ct)
            ?? throw new KeyNotFoundException($"Patient with ID {request.PatientId} not found.");

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId || d.UserId == doctorUserId, ct)
            ?? throw new KeyNotFoundException($"Doctor not found.");

        var encounter = new Encounter
        {
            HospitalId = hospitalId,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Type = request.Type,
            StartedAt = DateTime.UtcNow,
            Status = EncounterStatus.Active
        };

        _context.Encounters.Add(encounter);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "START",
            "Encounter",
            encounter.Id.ToString(),
            null,
            new { encounter.Id, encounter.PatientId, encounter.DoctorId, encounter.Type },
            ct);

        return await GetEncounterAsync(encounter.Id, ct);
    }

    public async Task<EncounterResponse> GetEncounterAsync(Guid encounterId, CancellationToken ct = default)
    {
        var encounter = await _context.Encounters
            .Include(e => e.Doctor)
            .Include(e => e.Vitals)
            .Include(e => e.Diagnoses)
            .Include(e => e.Notes)
            .Include(e => e.Prescriptions)
                .ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(e => e.Id == encounterId, ct)
            ?? throw new KeyNotFoundException($"Encounter with ID {encounterId} not found.");

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == encounter.PatientId, ct)
            ?? throw new KeyNotFoundException($"Patient record not found.");

        int? ageYears = patient.DateOfBirth.HasValue 
            ? DateTime.UtcNow.Year - patient.DateOfBirth.Value.Year 
            : null;

        var vitalsDtos = encounter.Vitals
            .OrderByDescending(v => v.RecordedAt)
            .Select(v => new VitalSignDto(
                v.Id,
                v.EncounterId,
                v.Temperature,
                v.Pulse,
                v.BpSystolic,
                v.BpDiastolic,
                v.BpSystolic.HasValue && v.BpDiastolic.HasValue ? $"{v.BpSystolic}/{v.BpDiastolic} mmHg" : null,
                v.SpO2,
                v.Weight,
                v.Height,
                (v.Weight.HasValue && v.Height.HasValue && v.Height > 0)
                    ? Math.Round(v.Weight.Value / ((v.Height.Value / 100m) * (v.Height.Value / 100m)), 1)
                    : null,
                v.RespiratoryRate,
                v.RecordedAt
            )).ToList();

        var diagnosesDtos = encounter.Diagnoses
            .Select(d => new DiagnosisDto(d.Id, d.EncounterId, d.IcdCode, d.Description, d.DescriptionBn, d.IsPrimary))
            .ToList();

        var notesDtos = encounter.Notes
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new ClinicalNoteDto(n.Id, n.EncounterId, n.Type, n.Body, n.AuthorId, n.CreatedAt))
            .ToList();

        var prescriptionsDtos = encounter.Prescriptions
            .OrderByDescending(p => p.IssuedAt)
            .Select(p => new PrescriptionResponse(
                p.Id,
                p.EncounterId,
                p.DoctorId,
                encounter.Doctor.FullName,
                encounter.Doctor.BmdcRegNo,
                encounter.Doctor.Qualification,
                encounter.Doctor.Specialty,
                patient.Id,
                $"{patient.FirstNameEn} {patient.LastNameEn}".Trim(),
                $"{patient.FirstNameBn} {patient.LastNameBn}".Trim(),
                patient.MRN,
                ageYears,
                patient.Gender.ToString(),
                encounter.Vitals.OrderByDescending(v => v.RecordedAt).FirstOrDefault()?.Weight,
                p.IssuedAt,
                p.Notes,
                p.FollowUpDate,
                p.Items.Select(i => new PrescriptionItemDto(
                    i.Id,
                    i.MedicineId,
                    i.MedicineName,
                    i.Dose,
                    i.Frequency,
                    i.DurationDays,
                    i.Instructions,
                    i.InstructionsBn
                )).ToList()
            )).ToList();

        return new EncounterResponse(
            encounter.Id,
            encounter.HospitalId,
            patient.Id,
            patient.MRN,
            $"{patient.FirstNameEn} {patient.LastNameEn}".Trim(),
            $"{patient.FirstNameBn} {patient.LastNameBn}".Trim(),
            ageYears,
            patient.Gender.ToString(),
            encounter.DoctorId,
            encounter.Doctor.FullName,
            encounter.Doctor.BmdcRegNo,
            encounter.Doctor.Specialty,
            encounter.Type.ToString(),
            encounter.Status.ToString(),
            encounter.StartedAt,
            encounter.EndedAt,
            vitalsDtos,
            diagnosesDtos,
            notesDtos,
            prescriptionsDtos
        );
    }

    public async Task<VitalSignDto> RecordVitalsAsync(
        Guid encounterId, 
        Guid recordedByUserId, 
        RecordVitalsRequest request, 
        CancellationToken ct = default)
    {
        var encounter = await _context.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, ct)
            ?? throw new KeyNotFoundException($"Encounter with ID {encounterId} not found.");

        var vital = new VitalSign
        {
            EncounterId = encounter.Id,
            Temperature = request.Temperature,
            Pulse = request.Pulse,
            BpSystolic = request.BpSystolic,
            BpDiastolic = request.BpDiastolic,
            SpO2 = request.SpO2,
            Weight = request.Weight,
            Height = request.Height,
            RespiratoryRate = request.RespiratoryRate,
            RecordedBy = recordedByUserId,
            RecordedAt = DateTime.UtcNow
        };

        _context.VitalSigns.Add(vital);
        await _context.SaveChangesAsync(ct);

        decimal? bmi = (vital.Weight.HasValue && vital.Height.HasValue && vital.Height > 0)
            ? Math.Round(vital.Weight.Value / ((vital.Height.Value / 100m) * (vital.Height.Value / 100m)), 1)
            : null;

        return new VitalSignDto(
            vital.Id,
            vital.EncounterId,
            vital.Temperature,
            vital.Pulse,
            vital.BpSystolic,
            vital.BpDiastolic,
            vital.BpSystolic.HasValue && vital.BpDiastolic.HasValue ? $"{vital.BpSystolic}/{vital.BpDiastolic} mmHg" : null,
            vital.SpO2,
            vital.Weight,
            vital.Height,
            bmi,
            vital.RespiratoryRate,
            vital.RecordedAt
        );
    }

    public async Task<DiagnosisDto> AddDiagnosisAsync(
        Guid encounterId, 
        AddDiagnosisRequest request, 
        CancellationToken ct = default)
    {
        var encounter = await _context.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, ct)
            ?? throw new KeyNotFoundException($"Encounter with ID {encounterId} not found.");

        var diag = new Diagnosis
        {
            EncounterId = encounter.Id,
            IcdCode = request.IcdCode,
            Description = request.Description,
            DescriptionBn = request.DescriptionBn,
            IsPrimary = request.IsPrimary
        };

        _context.Diagnoses.Add(diag);
        await _context.SaveChangesAsync(ct);

        return new DiagnosisDto(diag.Id, diag.EncounterId, diag.IcdCode, diag.Description, diag.DescriptionBn, diag.IsPrimary);
    }

    public async Task<ClinicalNoteDto> AddNoteAsync(
        Guid encounterId, 
        Guid authorUserId, 
        AddClinicalNoteRequest request, 
        CancellationToken ct = default)
    {
        var encounter = await _context.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, ct)
            ?? throw new KeyNotFoundException($"Encounter with ID {encounterId} not found.");

        var note = new ClinicalNote
        {
            EncounterId = encounter.Id,
            Type = request.Type,
            Body = request.Body,
            AuthorId = authorUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ClinicalNotes.Add(note);
        await _context.SaveChangesAsync(ct);

        return new ClinicalNoteDto(note.Id, note.EncounterId, note.Type, note.Body, note.AuthorId, note.CreatedAt);
    }

    public async Task<PrescriptionResponse> CreatePrescriptionAsync(
        Guid hospitalId, 
        Guid doctorUserId, 
        CreatePrescriptionRequest request, 
        CancellationToken ct = default)
    {
        // 1. Enforce that issuing user must have a valid BMDC registration number
        var user = await _userManager.FindByIdAsync(doctorUserId.ToString());
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == doctorUserId || d.Id == doctorUserId, ct);

        string? bmdc = user?.BmdcRegNo;
        if (string.IsNullOrWhiteSpace(bmdc) && doctor != null)
        {
            bmdc = doctor.BmdcRegNo;
        }

        if (string.IsNullOrWhiteSpace(bmdc))
        {
            throw new InvalidOperationException("Prescription cannot be issued: The issuing doctor does not possess a verified BMDC registration number.");
        }

        var encounter = await _context.Encounters
            .Include(e => e.Vitals)
            .FirstOrDefaultAsync(e => e.Id == request.EncounterId, ct)
            ?? throw new KeyNotFoundException($"Encounter with ID {request.EncounterId} not found.");

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == encounter.PatientId, ct)
            ?? throw new KeyNotFoundException($"Patient record not found.");

        doctor ??= await _context.Doctors.FirstOrDefaultAsync(d => d.Id == encounter.DoctorId, ct);
        if (doctor == null)
        {
            throw new InvalidOperationException("Doctor profile associated with encounter could not be located.");
        }

        var prescription = new Prescription
        {
            EncounterId = encounter.Id,
            DoctorId = doctor.Id,
            IssuedAt = DateTime.UtcNow,
            Notes = request.Notes,
            FollowUpDate = request.FollowUpDate
        };

        foreach (var item in request.Items)
        {
            prescription.Items.Add(new PrescriptionItem
            {
                MedicineId = item.MedicineId,
                MedicineName = item.MedicineName,
                Dose = item.Dose,
                Frequency = item.Frequency,
                DurationDays = item.DurationDays,
                Instructions = item.Instructions,
                InstructionsBn = item.InstructionsBn
            });
        }

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync(ct);

        // Audit every prescription write
        await _auditService.LogAsync(
            "CREATE",
            "Prescription",
            prescription.Id.ToString(),
            null,
            new 
            { 
                prescription.Id, 
                EncounterId = prescription.EncounterId, 
                DoctorBmdc = bmdc, 
                ItemCount = prescription.Items.Count,
                PatientMrn = patient.MRN
            },
            ct);

        int? ageYears = patient.DateOfBirth.HasValue 
            ? DateTime.UtcNow.Year - patient.DateOfBirth.Value.Year 
            : null;

        var latestVitals = encounter.Vitals.OrderByDescending(v => v.RecordedAt).FirstOrDefault();

        return new PrescriptionResponse(
            prescription.Id,
            prescription.EncounterId,
            doctor.Id,
            doctor.FullName,
            bmdc,
            doctor.Qualification,
            doctor.Specialty,
            patient.Id,
            $"{patient.FirstNameEn} {patient.LastNameEn}".Trim(),
            $"{patient.FirstNameBn} {patient.LastNameBn}".Trim(),
            patient.MRN,
            ageYears,
            patient.Gender.ToString(),
            latestVitals?.Weight,
            prescription.IssuedAt,
            prescription.Notes,
            prescription.FollowUpDate,
            prescription.Items.Select(i => new PrescriptionItemDto(
                i.Id,
                i.MedicineId,
                i.MedicineName,
                i.Dose,
                i.Frequency,
                i.DurationDays,
                i.Instructions,
                i.InstructionsBn
            )).ToList()
        );
    }

    public async Task<byte[]> GeneratePrescriptionPdfAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .Include(p => p.Encounter)
                .ThenInclude(e => e.Vitals)
            .Include(p => p.Encounter)
                .ThenInclude(e => e.Diagnoses)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, ct)
            ?? throw new KeyNotFoundException($"Prescription with ID {prescriptionId} not found.");

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == prescription.Encounter.PatientId, ct)
            ?? throw new KeyNotFoundException($"Patient record not found.");

        var hospital = await _context.Hospitals
            .FirstOrDefaultAsync(h => h.Id == prescription.Encounter.HospitalId, ct)
            ?? new Hospital
            {
                Name = "Sample Medical Centre, Dhaka (demo)",
                NameBn = "নমুনা মেডিকেল সেন্টার, ঢাকা (ডেমো)",
                Address = "House 12, Road 5, Dhanmondi, Dhaka-1205",
                BmdcOrLicenseNo = "DGHS-LIC-2026-DEMO",
                VatRegNo = "BIN-999888777-DEMO"
            };

        int? ageYears = patient.DateOfBirth.HasValue 
            ? DateTime.UtcNow.Year - patient.DateOfBirth.Value.Year 
            : null;

        var latestVitals = prescription.Encounter.Vitals.OrderByDescending(v => v.RecordedAt).FirstOrDefault();
        var primaryDiagnosis = prescription.Encounter.Diagnoses.FirstOrDefault(d => d.IsPrimary) 
                               ?? prescription.Encounter.Diagnoses.FirstOrDefault();

        // Build PDF with QuestPDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                // 1. Hospital Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(hospital.Name).FontSize(16).Bold().FontColor(Colors.Teal.Darken3);
                            c.Item().Text(hospital.NameBn ?? string.Empty).FontSize(11).FontColor(Colors.Teal.Darken2);
                            c.Item().Text(hospital.Address).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"DGHS Lic: {hospital.BmdcOrLicenseNo}").FontSize(8).FontColor(Colors.Grey.Darken2);
                            c.Item().Text($"VAT Reg: {hospital.VatRegNo}").FontSize(8).FontColor(Colors.Grey.Darken2);
                            c.Item().Text("Emergency Hotline: +880 1700 000000").FontSize(8).Bold().FontColor(Colors.Teal.Darken3);
                        });
                    });

                    col.Item().PaddingTop(6).LineHorizontal(2).LineColor(Colors.Teal.Darken2);

                    // Doctor Details Strip
                    col.Item().PaddingVertical(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(prescription.Doctor.FullName).FontSize(13).Bold().FontColor(Colors.Grey.Darken4);
                            if (!string.IsNullOrWhiteSpace(prescription.Doctor.Qualification))
                                c.Item().Text(prescription.Doctor.Qualification).FontSize(9).FontColor(Colors.Grey.Darken2);
                            c.Item().Text($"{prescription.Doctor.Specialty}").FontSize(9).FontColor(Colors.Teal.Darken3);
                            c.Item().Text($"BMDC Reg No: {prescription.Doctor.BmdcRegNo}").FontSize(9).Bold().FontColor(Colors.Red.Darken2);
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Consultation Date: {prescription.IssuedAt:dd-MMM-yyyy}").FontSize(9).SemiBold();
                            c.Item().Text($"Time: {prescription.IssuedAt:hh:mm tt} (BST)").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // Patient Details Box
                    col.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
                    {
                        row.RelativeItem(3).Column(c =>
                        {
                            c.Item().Text(txt =>
                            {
                                txt.Span("Patient: ").Bold();
                                txt.Span($"{patient.FirstNameEn} {patient.LastNameEn}");
                                if (!string.IsNullOrWhiteSpace(patient.FirstNameBn))
                                    txt.Span($" ({patient.FirstNameBn} {patient.LastNameBn})").FontColor(Colors.Grey.Darken2);
                            });
                            c.Item().Text(txt =>
                            {
                                txt.Span("MRN: ").Bold();
                                txt.Span(patient.MRN).FontColor(Colors.Teal.Darken3).Bold();
                            });
                        });

                        row.RelativeItem(2).Column(c =>
                        {
                            c.Item().Text(txt =>
                            {
                                txt.Span("Age / Sex: ").Bold();
                                txt.Span($"{ageYears?.ToString() ?? "N/A"} Yrs / {patient.Gender}");
                            });
                            if (latestVitals?.Weight.HasValue == true)
                            {
                                c.Item().Text(txt =>
                                {
                                    txt.Span("Weight: ").Bold();
                                    txt.Span($"{latestVitals.Weight.Value:F1} kg");
                                });
                            }
                        });

                        row.RelativeItem(2).Column(c =>
                        {
                            if (latestVitals != null)
                            {
                                if (latestVitals.BpSystolic.HasValue && latestVitals.BpDiastolic.HasValue)
                                    c.Item().Text($"BP: {latestVitals.BpSystolic}/{latestVitals.BpDiastolic} mmHg").FontSize(8.5f);
                                if (latestVitals.Pulse.HasValue)
                                    c.Item().Text($"Pulse: {latestVitals.Pulse} bpm").FontSize(8.5f);
                                if (latestVitals.Temperature.HasValue)
                                    c.Item().Text($"Temp: {latestVitals.Temperature:F1} °F").FontSize(8.5f);
                            }
                        });
                    });

                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // 2. Body / Clinical Content
                page.Content().PaddingVertical(10).Row(mainRow =>
                {
                    // Left Column: Clinical Diagnosis & Vitals
                    mainRow.ConstantItem(160).PaddingRight(10).Column(c =>
                    {
                        c.Item().Text("CLINICAL DIAGNOSIS").FontSize(9).Bold().FontColor(Colors.Teal.Darken3);
                        if (primaryDiagnosis != null)
                        {
                            c.Item().PaddingTop(2).Text(txt =>
                            {
                                if (!string.IsNullOrWhiteSpace(primaryDiagnosis.IcdCode))
                                    txt.Span($"[{primaryDiagnosis.IcdCode}] ").Bold();
                                txt.Span(primaryDiagnosis.Description);
                            });
                            if (!string.IsNullOrWhiteSpace(primaryDiagnosis.DescriptionBn))
                            {
                                c.Item().Text(primaryDiagnosis.DescriptionBn).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            }
                        }
                        else
                        {
                            c.Item().Text("General OPD consultation").FontSize(8.5f).Italic();
                        }

                        if (!string.IsNullOrWhiteSpace(prescription.Notes))
                        {
                            c.Item().PaddingTop(12).Text("CLINICAL NOTES").FontSize(9).Bold().FontColor(Colors.Teal.Darken3);
                            c.Item().PaddingTop(2).Text(prescription.Notes).FontSize(8.5f);
                        }

                        c.Item().PaddingTop(20).Text("VITALS SUMMARY").FontSize(9).Bold().FontColor(Colors.Teal.Darken3);
                        if (latestVitals != null)
                        {
                            if (latestVitals.BpSystolic.HasValue)
                                c.Item().Text($"• BP: {latestVitals.BpSystolic}/{latestVitals.BpDiastolic} mmHg").FontSize(8);
                            if (latestVitals.Pulse.HasValue)
                                c.Item().Text($"• Pulse: {latestVitals.Pulse} bpm").FontSize(8);
                            if (latestVitals.Temperature.HasValue)
                                c.Item().Text($"• Temp: {latestVitals.Temperature:F1} °F").FontSize(8);
                            if (latestVitals.SpO2.HasValue)
                                c.Item().Text($"• SpO2: {latestVitals.SpO2} %").FontSize(8);
                            if (latestVitals.Weight.HasValue)
                                c.Item().Text($"• Weight: {latestVitals.Weight:F1} kg").FontSize(8);
                        }
                    });

                    // Divider line
                    mainRow.ConstantItem(1).LineVertical(1).LineColor(Colors.Grey.Lighten2);

                    // Right Column: Rx Prescriptions
                    mainRow.RelativeItem().PaddingLeft(15).Column(c =>
                    {
                        c.Item().Text("℞").FontSize(24).Bold().FontColor(Colors.Teal.Darken3);

                        int itemIndex = 1;
                        foreach (var item in prescription.Items)
                        {
                            c.Item().PaddingBottom(10).Column(itemCol =>
                            {
                                itemCol.Item().Row(r =>
                                {
                                    r.AutoItem().Text($"{itemIndex}. ").Bold().FontSize(10.5f);
                                    r.RelativeItem().Text(item.MedicineName).Bold().FontSize(10.5f).FontColor(Colors.Grey.Darken4);
                                });

                                itemCol.Item().PaddingLeft(14).Row(r =>
                                {
                                    if (!string.IsNullOrWhiteSpace(item.Dose))
                                        r.AutoItem().PaddingRight(8).Text(item.Dose).FontSize(9).FontColor(Colors.Grey.Darken2);
                                    if (!string.IsNullOrWhiteSpace(item.Frequency))
                                        r.AutoItem().PaddingRight(8).Text($"— {item.Frequency}").FontSize(9).Bold().FontColor(Colors.Teal.Darken3);
                                    if (item.DurationDays.HasValue)
                                        r.AutoItem().Text($"— {item.DurationDays.Value} দিন / days").FontSize(9).FontColor(Colors.Grey.Darken2);
                                });

                                if (!string.IsNullOrWhiteSpace(item.Instructions) || !string.IsNullOrWhiteSpace(item.InstructionsBn))
                                {
                                    string instructions = string.Join(" / ", new[] { item.InstructionsBn, item.Instructions }.Where(s => !string.IsNullOrWhiteSpace(s)));
                                    itemCol.Item().PaddingLeft(14).Text($"({instructions})").FontSize(8.5f).Italic().FontColor(Colors.Grey.Darken2);
                                }
                            });
                            itemIndex++;
                        }

                        if (prescription.FollowUpDate.HasValue)
                        {
                            c.Item().PaddingTop(15).Background(Colors.Teal.Lighten5).Padding(8).Text(txt =>
                            {
                                txt.Span("Follow-up / পরবর্তী সাক্ষাত: ").Bold().FontColor(Colors.Teal.Darken3);
                                txt.Span($"{prescription.FollowUpDate.Value:dd-MMM-yyyy} তারিখে আসবেন।");
                            });
                        }
                    });
                });

                // 3. Footer
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Please bring this prescription for every follow-up visit.").FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1);
                            c.Item().Text("এই প্রেসক্রিপশনটি পরবর্তী প্রতিটি পরিদর্শনে সঙ্গে আনবেন।").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            c.Item().Text("Generated by Bangladesh HMS • Confidential Medical Record").FontSize(7).FontColor(Colors.Grey.Lighten1);
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().PaddingTop(12).LineHorizontal(100).LineColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(2).Text(prescription.Doctor.FullName).FontSize(8.5f).Bold();
                            c.Item().Text($"BMDC Reg: {prescription.Doctor.BmdcRegNo}").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public Task<IReadOnlyList<Icd10CatalogueItemDto>> SearchIcd10Async(string? query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(Icd10Dataset.Items);
        }

        var normalized = query.Trim().ToLowerInvariant();
        IReadOnlyList<Icd10CatalogueItemDto> filtered = Icd10Dataset.Items
            .Where(i => i.Code.ToLowerInvariant().Contains(normalized) ||
                        i.Description.ToLowerInvariant().Contains(normalized) ||
                        (i.DescriptionBn != null && i.DescriptionBn.Contains(normalized)) ||
                        i.Category.ToLowerInvariant().Contains(normalized))
            .ToList();

        return Task.FromResult(filtered);
    }

    public async Task<IReadOnlyList<MedicineCatalogueItemDto>> SearchMedicinesAsync(string? query, CancellationToken ct = default)
    {
        var dbQuery = _context.Medicines.Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            dbQuery = dbQuery.Where(m => m.GenericName.ToLower().Contains(q) || m.BrandName.ToLower().Contains(q));
        }

        return await dbQuery
            .OrderBy(m => m.GenericName)
            .Take(50)
            .Select(m => new MedicineCatalogueItemDto(
                m.Id,
                m.GenericName,
                m.BrandName,
                m.Strength,
                m.Form.ToString(),
                m.Unit
            ))
            .ToListAsync(ct);
    }
}

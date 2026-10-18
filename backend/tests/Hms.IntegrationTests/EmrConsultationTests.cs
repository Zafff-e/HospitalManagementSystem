using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Hms.Application.Appointments.Dtos;
using Hms.Application.Emr.Dtos;
using Hms.Application.Patients.Dtos;
using Hms.Domain.Enums;
using Xunit;

namespace Hms.IntegrationTests;

public class EmrConsultationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public EmrConsultationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync(string email, string password = "Password123!")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new Hms.Application.Auth.Dtos.LoginRequest(
            email,
            password
        ));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Hms.Application.Auth.Dtos.LoginResponse>();
        return result!.AccessToken;
    }

    [Fact]
    public async Task NonDoctor_CannotPrescribe_Returns403Forbidden()
    {
        // Receptionist does NOT have 'emr.prescribe' permission
        var receptionistToken = await GetTokenAsync("receptionist@demo-hms.local");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", receptionistToken);

        var dummyEncounterId = Guid.NewGuid();
        var request = new CreatePrescriptionRequest(
            dummyEncounterId,
            "Rest and hydration",
            DateTime.UtcNow.AddDays(7),
            new List<PrescriptionItemRequest>
            {
                new(null, "Paracetamol 500mg", "1 tab", "1+0+1", 5, "After meal", "ভরা পেটে")
            }
        );

        var response = await _client.PostAsJsonAsync($"/api/v1/encounters/{dummyEncounterId}/prescriptions", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Doctor_FullConsultationFlow_Succeeds_AndGeneratesPdf()
    {
        // 1. Doctor Logs in
        var doctorToken = await GetTokenAsync("doctor@demo-hms.local");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", doctorToken);

        // 2. Fetch or create a patient
        var patientsRes = await _client.GetAsync("/api/v1/patients?page=1&pageSize=5");
        patientsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedPatients = await patientsRes.Content.ReadFromJsonAsync<PagedResult<PatientResponse>>();
        pagedPatients.Should().NotBeNull();
        pagedPatients!.Items.Should().NotBeEmpty();
        var patient = pagedPatients.Items.First();

        // 3. Fetch doctor list to get doctor ID
        var doctorsRes = await _client.GetAsync("/api/v1/appointments/doctors");
        doctorsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var doctors = await doctorsRes.Content.ReadFromJsonAsync<List<DoctorDto>>();
        doctors.Should().NotBeNull();
        doctors.Should().NotBeEmpty();
        var doctor = doctors!.First();

        // 4. Start Encounter
        var startEncReq = new CreateEncounterRequest(patient.Id, doctor.Id, EncounterType.OPD);
        var encRes = await _client.PostAsJsonAsync("/api/v1/encounters", startEncReq);
        encRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var encounter = await encRes.Content.ReadFromJsonAsync<EncounterResponse>();
        encounter.Should().NotBeNull();
        encounter!.PatientId.Should().Be(patient.Id);

        // 5. Record Vitals
        var vitalsReq = new RecordVitalsRequest(
            Temperature: 98.6m,
            Pulse: 74,
            BpSystolic: 120,
            BpDiastolic: 80,
            SpO2: 99,
            Weight: 70.5m,
            Height: 172.0m,
            RespiratoryRate: 16
        );
        var vitalsRes = await _client.PostAsJsonAsync($"/api/v1/encounters/{encounter.Id}/vitals", vitalsReq);
        vitalsRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var vitals = await vitalsRes.Content.ReadFromJsonAsync<VitalSignDto>();
        vitals.Should().NotBeNull();
        vitals!.BpString.Should().Be("120/80 mmHg");
        vitals.Bmi.Should().BeApproximately(23.8m, 0.5m);

        // 6. Record Diagnosis
        var diagReq = new AddDiagnosisRequest(
            IcdCode: "I10",
            Description: "Essential (primary) hypertension",
            DescriptionBn: "উচ্চ রক্তচাপ",
            IsPrimary: true
        );
        var diagRes = await _client.PostAsJsonAsync($"/api/v1/encounters/{encounter.Id}/diagnoses", diagReq);
        diagRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var diag = await diagRes.Content.ReadFromJsonAsync<DiagnosisDto>();
        diag.Should().NotBeNull();
        diag!.IcdCode.Should().Be("I10");

        // 7. Prescribe Two Medicines (BMDC guarded)
        var presReq = new CreatePrescriptionRequest(
            EncounterId: encounter.Id,
            Notes: "Avoid excess salt, daily 30 minutes brisk walking.",
            FollowUpDate: DateTime.UtcNow.AddDays(14),
            Items: new List<PrescriptionItemRequest>
            {
                new(null, "Losartan Potassium 50mg (Angilock)", "1 tablet", "1+0+0", 30, "Morning after breakfast", "সকালে খাবার পর"),
                new(null, "Amlodipine 5mg (Camlodin)", "1 tablet", "0+0+1", 30, "Night after dinner", "রাতে খাবার পর")
            }
        );
        var presRes = await _client.PostAsJsonAsync($"/api/v1/encounters/{encounter.Id}/prescriptions", presReq);
        presRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var prescription = await presRes.Content.ReadFromJsonAsync<PrescriptionResponse>();
        prescription.Should().NotBeNull();
        prescription!.DoctorBmdc.Should().NotBeNullOrWhiteSpace();
        prescription.Items.Should().HaveCount(2);
        prescription.PatientMrn.Should().NotBeNullOrWhiteSpace();

        // 8. Download Prescription PDF
        var pdfRes = await _client.GetAsync($"/api/v1/prescriptions/{prescription.Id}/pdf");
        var errorBody = pdfRes.IsSuccessStatusCode ? null : await pdfRes.Content.ReadAsStringAsync();
        pdfRes.StatusCode.Should().Be(HttpStatusCode.OK, because: errorBody ?? "Success");
        pdfRes.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var pdfBytes = await pdfRes.Content.ReadAsByteArrayAsync();
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(1000);

        // Verify PDF Header magic number (%PDF)
        var pdfHeader = Encoding.ASCII.GetString(pdfBytes, 0, 4);
        pdfHeader.Should().Be("%PDF");
    }

    [Fact]
    public async Task ClinicalCatalogs_Search_ReturnsExpectedResults()
    {
        var doctorToken = await GetTokenAsync("doctor@demo-hms.local");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", doctorToken);

        // 1. Search ICD-10
        var icdRes = await _client.GetAsync("/api/v1/clinical/icd10?q=dengue");
        icdRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var icdItems = await icdRes.Content.ReadFromJsonAsync<List<Icd10CatalogueItemDto>>();
        icdItems.Should().NotBeNull();
        icdItems.Should().ContainSingle(i => i.Code == "A90");

        // 2. Search Medicines
        var medRes = await _client.GetAsync("/api/v1/clinical/medicines?q=napa");
        medRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var medItems = await medRes.Content.ReadFromJsonAsync<List<MedicineCatalogueItemDto>>();
        medItems.Should().NotBeNull();
        medItems.Should().Contain(m => m.BrandName.ToLower().Contains("napa"));
    }
}

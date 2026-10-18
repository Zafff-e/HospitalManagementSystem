using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Hms.Application.Auth.Dtos;
using Hms.Application.Patients.Dtos;
using Hms.Domain.Enums;
using Xunit;

namespace Hms.IntegrationTests;

public class PatientRegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PatientRegistrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetReceptionistTokenAsync()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("receptionist@demo-hms.local", "Password123!"));
        var login = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.AccessToken;
    }

    [Fact]
    public async Task CreatePatient_ValidInput_ShouldCreateAndReturnMrn()
    {
        var token = await GetReceptionistTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreatePatientRequest(
            FirstNameEn: "Tahsin",
            LastNameEn: "Rahman",
            FirstNameBn: "তাহসিন",
            LastNameBn: "রহমান",
            DateOfBirth: new DateTime(1995, 6, 15),
            Gender: Gender.Male,
            BloodGroup: BloodGroup.BPositive,
            PhonePrimary: "01755123456",
            PhoneSecondary: null,
            Email: "tahsin@example.local",
            NationalId: "1995123456789", // 13 digits
            BirthRegNo: null,
            DivisionId: 1,
            DistrictId: 1,
            UpazilaId: 1,
            AddressLine: "House 24, Road 8, Dhanmondi",
            Occupation: "Software Engineer",
            MaritalStatus: MaritalStatus.Single,
            Religion: "Islam",
            EmergencyContact: new PatientEmergencyContactDto("Kawsar Rahman", "Brother", "+8801755000000")
        );

        var response = await _client.PostAsJsonAsync("/api/v1/patients", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        patient.Should().NotBeNull();
        patient!.MRN.Should().MatchRegex(@"^\d{4}-\d{6}$");
        patient.PhonePrimary.Should().Be("+8801755123456"); // E.164 normalized
        patient.NidLast4.Should().Be("6789");
        patient.DisplayName.Should().Be("Tahsin Rahman");
        patient.AgeYears.Should().BeGreaterThan(25);
    }

    [Fact]
    public async Task CreatePatient_DateOfBirthInFuture_ShouldReturnBadRequest()
    {
        var token = await GetReceptionistTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreatePatientRequest(
            FirstNameEn: "Future",
            LastNameEn: "Baby",
            FirstNameBn: null,
            LastNameBn: null,
            DateOfBirth: DateTime.UtcNow.AddYears(1), // Future date!
            Gender: Gender.Male,
            BloodGroup: BloodGroup.OPositive,
            PhonePrimary: "01811223344",
            PhoneSecondary: null,
            Email: null,
            NationalId: null,
            BirthRegNo: null,
            DivisionId: null,
            DistrictId: null,
            UpazilaId: null,
            AddressLine: null,
            Occupation: null,
            MaritalStatus: null,
            Religion: null,
            EmergencyContact: null
        );

        var response = await _client.PostAsJsonAsync("/api/v1/patients", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("future");
    }

    [Fact]
    public async Task CreatePatient_MissingBothEnAndBnNames_ShouldReturnBadRequest()
    {
        var token = await GetReceptionistTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreatePatientRequest(
            FirstNameEn: null,
            LastNameEn: null,
            FirstNameBn: null,
            LastNameBn: null,
            DateOfBirth: new DateTime(1990, 1, 1),
            Gender: Gender.Female,
            BloodGroup: BloodGroup.APositive,
            PhonePrimary: "01799887766",
            PhoneSecondary: null,
            Email: null,
            NationalId: null,
            BirthRegNo: null,
            DivisionId: null,
            DistrictId: null,
            UpazilaId: null,
            AddressLine: null,
            Occupation: null,
            MaritalStatus: null,
            Religion: null,
            EmergencyContact: null
        );

        var response = await _client.PostAsJsonAsync("/api/v1/patients", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("name is required");
    }

    [Fact]
    public async Task CreatePatient_DuplicatePhone_ShouldReturnDuplicateWarningFlag()
    {
        var token = await GetReceptionistTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const string existingPhone = "01788776655";

        // First registration
        var req1 = new CreatePatientRequest(
            FirstNameEn: "Original",
            LastNameEn: "Patient",
            FirstNameBn: null,
            LastNameBn: null,
            DateOfBirth: new DateTime(1988, 5, 10),
            Gender: Gender.Male,
            BloodGroup: BloodGroup.BPositive,
            PhonePrimary: existingPhone,
            PhoneSecondary: null,
            Email: null,
            NationalId: null,
            BirthRegNo: null,
            DivisionId: null,
            DistrictId: null,
            UpazilaId: null,
            AddressLine: null,
            Occupation: null,
            MaritalStatus: null,
            Religion: null,
            EmergencyContact: null
        );
        var res1 = await _client.PostAsJsonAsync("/api/v1/patients", req1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second registration with the same phone: Succeeds (not blocked) but has duplicate warning!
        var req2 = new CreatePatientRequest(
            FirstNameEn: "Duplicate",
            LastNameEn: "Candidate",
            FirstNameBn: null,
            LastNameBn: null,
            DateOfBirth: new DateTime(1992, 8, 20),
            Gender: Gender.Female,
            BloodGroup: BloodGroup.OPositive,
            PhonePrimary: existingPhone,
            PhoneSecondary: null,
            Email: null,
            NationalId: null,
            BirthRegNo: null,
            DivisionId: null,
            DistrictId: null,
            UpazilaId: null,
            AddressLine: null,
            Occupation: null,
            MaritalStatus: null,
            Religion: null,
            EmergencyContact: null
        );
        var res2 = await _client.PostAsJsonAsync("/api/v1/patients", req2);
        res2.StatusCode.Should().Be(HttpStatusCode.Created);

        var patient2 = await res2.Content.ReadFromJsonAsync<PatientResponse>();
        patient2.Should().NotBeNull();
        patient2!.DuplicateWarning.Should().NotBeNull();
        patient2.DuplicateWarning!.HasDuplicateWarning.Should().BeTrue();
        patient2.DuplicateWarning.Reason.Should().Contain("phone");
    }

    [Fact]
    public async Task SearchPatients_WithSeededData_ShouldReturnPaginatedResults()
    {
        var token = await GetReceptionistTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Search with query "Mohammad" from the 200 seeded patients
        var response = await _client.GetAsync("/api/v1/patients?query=Mohammad&page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<PatientResponse>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.TotalCount.Should().BeGreaterThan(0);
        pagedResult.Items.Should().NotBeEmpty();
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(10);
    }
}

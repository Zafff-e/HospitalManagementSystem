using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Hms.Application.Appointments.Dtos;
using Hms.Application.Patients.Dtos;
using Hms.Domain.Enums;
using Xunit;

namespace Hms.IntegrationTests;

public class AppointmentsAndOpdTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AppointmentsAndOpdTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync(string email = "receptionist@demo-hms.local", string password = "Password123!")
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
    public async Task GetAvailableSlots_ShouldReturnSlots_ForDoctorSchedule()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Fetch doctors
        var doctorsRes = await _client.GetAsync("/api/v1/appointments/doctors");
        doctorsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var doctors = await doctorsRes.Content.ReadFromJsonAsync<List<DoctorDto>>();
        doctors.Should().NotBeNull();
        doctors.Should().NotBeEmpty();

        var doctor = doctors!.First();

        // Find next Sunday (Sunday is in the doctor's active schedule)
        var nextSunday = DateTime.UtcNow.Date.AddDays(1);
        while (nextSunday.DayOfWeek != DayOfWeek.Sunday)
        {
            nextSunday = nextSunday.AddDays(1);
        }

        var slotsRes = await _client.GetAsync($"/api/v1/appointments/slots?doctorId={doctor.Id}&date={nextSunday:yyyy-MM-dd}");
        slotsRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await slotsRes.Content.ReadFromJsonAsync<List<TimeSlotDto>>();
        slots.Should().NotBeNull();
        slots.Should().NotBeEmpty();
        slots!.All(s => s.IsAvailable).Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentBookings_ForSameSlot_ShouldReturnOneSuccess_AndOneConflict()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get two distinct patients from seeded data
        var patientsRes = await _client.GetAsync("/api/v1/patients?page=1&pageSize=2");
        var patientsPaged = await patientsRes.Content.ReadFromJsonAsync<PagedResult<PatientResponse>>();
        patientsPaged.Should().NotBeNull();
        patientsPaged!.Items.Count.Should().BeGreaterThanOrEqualTo(2);

        var p1 = patientsPaged.Items[0];
        var p2 = patientsPaged.Items[1];

        // Doctor 1 (Dr. Anisur Rahman)
        var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Schedule at 10:00 AM on next Monday
        var nextMonday = DateTime.UtcNow.Date.AddDays(1);
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        var targetSlotTime = nextMonday.AddHours(10); // 10:00 AM

        var req1 = new BookAppointmentRequest(p1.Id, doctorId, targetSlotTime, "General checkup");
        var req2 = new BookAppointmentRequest(p2.Id, doctorId, targetSlotTime, "Second opinion");

        // Concurrent execution
        var task1 = _client.PostAsJsonAsync("/api/v1/appointments", req1);
        var task2 = _client.PostAsJsonAsync("/api/v1/appointments", req2);

        var responses = await Task.WhenAll(task1, task2);

        var statusCodes = responses.Select(r => r.StatusCode).ToList();

        // Exactly one must be 201 Created and the other must be 409 Conflict
        statusCodes.Should().Contain(HttpStatusCode.Created);
        statusCodes.Should().Contain(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CheckIn_ShouldAssignSequentialToken_AndReflectInQueue()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Fetch a patient
        var patientsRes = await _client.GetAsync("/api/v1/patients?page=1&pageSize=1");
        var patientsPaged = await patientsRes.Content.ReadFromJsonAsync<PagedResult<PatientResponse>>();
        var patient = patientsPaged!.Items[0];

        var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var nextTuesday = DateTime.UtcNow.Date.AddDays(1);
        while (nextTuesday.DayOfWeek != DayOfWeek.Tuesday)
        {
            nextTuesday = nextTuesday.AddDays(1);
        }
        var slotTime = nextTuesday.AddHours(11);

        // Book appointment
        var bookRes = await _client.PostAsJsonAsync("/api/v1/appointments", new BookAppointmentRequest(
            patient.Id,
            doctorId,
            slotTime,
            "Follow-up"
        ));
        bookRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await bookRes.Content.ReadFromJsonAsync<AppointmentResponse>();
        appointment.Should().NotBeNull();

        // Check in
        var checkInRes = await _client.PostAsJsonAsync($"/api/v1/appointments/{appointment!.Id}/check-in", new { });
        checkInRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkIn = await checkInRes.Content.ReadFromJsonAsync<CheckInResponse>();
        checkIn.Should().NotBeNull();
        checkIn!.TokenNo.Should().BeGreaterThan(0);
        checkIn.PatientMrn.Should().Be(patient.MRN);

        // Verify Doctor Queue
        var queueRes = await _client.GetAsync($"/api/v1/appointments/queue?doctorId={doctorId}&date={slotTime:yyyy-MM-dd}");
        queueRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var queue = await queueRes.Content.ReadFromJsonAsync<List<OpdQueueItemDto>>();
        queue.Should().NotBeNull();
        queue!.Any(q => q.AppointmentId == appointment.Id && q.TokenNo == checkIn.TokenNo).Should().BeTrue();
    }
}

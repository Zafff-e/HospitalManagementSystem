using Microsoft.AspNetCore.Mvc;
using Hms.Application.Appointments.Dtos;
using Hms.Application.Common.Interfaces;
using Hms.Infrastructure.Authorization;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    private Guid GetHospitalId()
    {
        var hospitalClaim = User.FindFirst("hospital_id")?.Value;
        if (Guid.TryParse(hospitalClaim, out var hospitalId))
        {
            return hospitalId;
        }

        return Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    [HttpGet("doctors")]
    [HasPermission("appt.read")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors()
    {
        var hospitalId = GetHospitalId();
        var doctors = await _appointmentService.GetDoctorsAsync(hospitalId, HttpContext.RequestAborted);
        return Ok(doctors);
    }

    [HttpGet("slots")]
    [HasPermission("appt.read")]
    [ProducesResponseType(typeof(IReadOnlyList<TimeSlotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlots([FromQuery] Guid doctorId, [FromQuery] DateTime date)
    {
        var slots = await _appointmentService.GetAvailableSlotsAsync(doctorId, date, HttpContext.RequestAborted);
        return Ok(slots);
    }

    [HttpGet]
    [HasPermission("appt.read")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] DateTime? date,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? patientId)
    {
        var hospitalId = GetHospitalId();
        var appointments = await _appointmentService.GetAppointmentsAsync(hospitalId, date, doctorId, patientId, HttpContext.RequestAborted);
        return Ok(appointments);
    }

    [HttpPost]
    [HasPermission("appt.write")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request)
    {
        var hospitalId = GetHospitalId();
        var appointment = await _appointmentService.BookAppointmentAsync(hospitalId, request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetAppointments), new { id = appointment.Id }, appointment);
    }

    [HttpPost("{id:guid}/check-in")]
    [HasPermission("appt.checkin")]
    [ProducesResponseType(typeof(CheckInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(Guid id)
    {
        var hospitalId = GetHospitalId();
        var result = await _appointmentService.CheckInAsync(hospitalId, id, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("queue")]
    [HasPermission("appt.read")]
    [ProducesResponseType(typeof(IReadOnlyList<OpdQueueItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorQueue([FromQuery] Guid doctorId, [FromQuery] DateTime date)
    {
        var queue = await _appointmentService.GetDoctorQueueAsync(doctorId, date, HttpContext.RequestAborted);
        return Ok(queue);
    }
}

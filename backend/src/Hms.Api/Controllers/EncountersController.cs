using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hms.Application.Common.Interfaces;
using Hms.Application.Emr.Dtos;
using Hms.Infrastructure.Authorization;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class EncountersController : ControllerBase
{
    private readonly IEmrService _emrService;

    public EncountersController(IEmrService emrService)
    {
        _emrService = emrService;
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

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(sub, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }

    [HttpPost]
    [HasPermission("emr.write")]
    [ProducesResponseType(typeof(EncounterResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> StartEncounter([FromBody] CreateEncounterRequest request)
    {
        var hospitalId = GetHospitalId();
        var userId = GetUserId();
        var response = await _emrService.StartEncounterAsync(hospitalId, userId, request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetEncounter), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("emr.read")]
    [ProducesResponseType(typeof(EncounterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEncounter(Guid id)
    {
        var response = await _emrService.GetEncounterAsync(id, HttpContext.RequestAborted);
        return Ok(response);
    }

    [HttpPost("{id:guid}/vitals")]
    [HasPermission("emr.write")]
    [ProducesResponseType(typeof(VitalSignDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordVitals(Guid id, [FromBody] RecordVitalsRequest request)
    {
        var userId = GetUserId();
        var result = await _emrService.RecordVitalsAsync(id, userId, request, HttpContext.RequestAborted);
        return Created($"/api/v1/encounters/{id}/vitals/{result.Id}", result);
    }

    [HttpPost("{id:guid}/diagnoses")]
    [HasPermission("emr.write")]
    [ProducesResponseType(typeof(DiagnosisDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddDiagnosis(Guid id, [FromBody] AddDiagnosisRequest request)
    {
        var result = await _emrService.AddDiagnosisAsync(id, request, HttpContext.RequestAborted);
        return Created($"/api/v1/encounters/{id}/diagnoses/{result.Id}", result);
    }

    [HttpPost("{id:guid}/notes")]
    [HasPermission("emr.write")]
    [ProducesResponseType(typeof(ClinicalNoteDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddClinicalNoteRequest request)
    {
        var userId = GetUserId();
        var result = await _emrService.AddNoteAsync(id, userId, request, HttpContext.RequestAborted);
        return Created($"/api/v1/encounters/{id}/notes/{result.Id}", result);
    }

    [HttpPost("{id:guid}/prescriptions")]
    [HasPermission("emr.prescribe")]
    [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePrescription(Guid id, [FromBody] CreatePrescriptionRequest request)
    {
        var hospitalId = GetHospitalId();
        var userId = GetUserId();
        
        // Ensure route encounter id matches payload
        var command = request with { EncounterId = id };
        var result = await _emrService.CreatePrescriptionAsync(hospitalId, userId, command, HttpContext.RequestAborted);
        return Created($"/api/v1/prescriptions/{result.Id}", result);
    }
}

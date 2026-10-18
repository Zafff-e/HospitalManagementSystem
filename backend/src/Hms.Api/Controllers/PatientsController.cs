using Microsoft.AspNetCore.Mvc;
using Hms.Application.Common.Interfaces;
using Hms.Application.Patients.Dtos;
using Hms.Infrastructure.Authorization;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    private Guid GetHospitalId()
    {
        var hospitalClaim = User.FindFirst("hospital_id")?.Value;
        if (Guid.TryParse(hospitalClaim, out var hospitalId))
        {
            return hospitalId;
        }

        // Default to demo hospital if not authenticated or not in token
        return Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    [HttpGet]
    [HasPermission("patient.read")]
    [ProducesResponseType(typeof(PagedResult<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] string? phone,
        [FromQuery] string? nidLast4,
        [FromQuery] string? mrn,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var searchQuery = new PatientSearchQuery
        {
            Query = query,
            Phone = phone,
            NidLast4 = nidLast4,
            Mrn = mrn,
            Page = page,
            PageSize = pageSize
        };
        var hospitalId = GetHospitalId();
        var result = await _patientService.SearchPatientsAsync(hospitalId, searchQuery, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("patient.read")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var hospitalId = GetHospitalId();
        var patient = await _patientService.GetPatientByIdAsync(hospitalId, id, HttpContext.RequestAborted);
        if (patient == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Patient not found",
                Detail = $"Patient with ID '{id}' was not found in this hospital."
            });
        }
        return Ok(patient);
    }

    [HttpPost]
    [HasPermission("patient.write")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var hospitalId = GetHospitalId();
        var response = await _patientService.CreatePatientAsync(hospitalId, request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("patient.write")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request)
    {
        var hospitalId = GetHospitalId();
        var response = await _patientService.UpdatePatientAsync(hospitalId, id, request, HttpContext.RequestAborted);
        return Ok(response);
    }

    [HttpPost("check-duplicate")]
    [HasPermission("patient.read")]
    [ProducesResponseType(typeof(DuplicateWarningDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckDuplicate([FromBody] CheckDuplicateRequest request)
    {
        var hospitalId = GetHospitalId();
        var warning = await _patientService.CheckDuplicateAsync(hospitalId, request.Phone, request.NidLast4, request.DateOfBirth, HttpContext.RequestAborted);
        return Ok(warning);
    }
}

public record CheckDuplicateRequest(string Phone, string? NidLast4, DateTime? DateOfBirth);

using Microsoft.AspNetCore.Mvc;
using Hms.Application.Common.Interfaces;
using Hms.Application.Emr.Dtos;
using Hms.Infrastructure.Authorization;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ClinicalController : ControllerBase
{
    private readonly IEmrService _emrService;

    public ClinicalController(IEmrService emrService)
    {
        _emrService = emrService;
    }

    [HttpGet("icd10")]
    [HasPermission("emr.read")]
    [ProducesResponseType(typeof(IReadOnlyList<Icd10CatalogueItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchIcd10([FromQuery] string? q)
    {
        var results = await _emrService.SearchIcd10Async(q, HttpContext.RequestAborted);
        return Ok(results);
    }

    [HttpGet("medicines")]
    [HasPermission("emr.read")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicineCatalogueItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMedicines([FromQuery] string? q)
    {
        var results = await _emrService.SearchMedicinesAsync(q, HttpContext.RequestAborted);
        return Ok(results);
    }
}

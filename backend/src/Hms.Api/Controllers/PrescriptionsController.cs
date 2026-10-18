using Microsoft.AspNetCore.Mvc;
using Hms.Application.Common.Interfaces;
using Hms.Infrastructure.Authorization;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly IEmrService _emrService;

    public PrescriptionsController(IEmrService emrService)
    {
        _emrService = emrService;
    }

    [HttpGet("{id:guid}/pdf")]
    [HasPermission("emr.read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrescriptionPdf(Guid id)
    {
        var pdfBytes = await _emrService.GeneratePrescriptionPdfAsync(id, HttpContext.RequestAborted);
        return File(pdfBytes, "application/pdf", $"Prescription_{id}.pdf");
    }
}

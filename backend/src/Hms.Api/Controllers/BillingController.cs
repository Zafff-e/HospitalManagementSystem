using Microsoft.AspNetCore.Mvc;
using Hms.Infrastructure.Authorization;

namespace Hms.Api.Controllers;

public record RefundTestRequest(Guid PaymentId, decimal Amount, string Reason);

[ApiController]
[Route("api/v1/[controller]")]
public class BillingController : ControllerBase
{
    [HttpPost("refund")]
    [HasPermission("billing.refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ProcessRefund([FromBody] RefundTestRequest request)
    {
        return Ok(new
        {
            Status = "RefundApproved",
            PaymentId = request.PaymentId,
            Amount = request.Amount,
            Reason = request.Reason,
            ProcessedAt = DateTime.UtcNow
        });
    }
}

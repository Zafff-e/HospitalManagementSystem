using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hms.Infrastructure.Persistence;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class GeoController : ControllerBase
{
    private readonly HmsDbContext _db;

    public GeoController(HmsDbContext db)
    {
        _db = db;
    }

    [HttpGet("divisions")]
    public async Task<IActionResult> GetDivisions()
    {
        var divisions = await _db.Divisions
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, d.NameBn })
            .ToListAsync();
        return Ok(divisions);
    }

    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts([FromQuery] int? divisionId)
    {
        var query = _db.Districts.AsQueryable();
        if (divisionId.HasValue)
        {
            query = query.Where(d => d.DivisionId == divisionId.Value);
        }

        var districts = await query
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.DivisionId, d.Name, d.NameBn })
            .ToListAsync();
        return Ok(districts);
    }

    [HttpGet("upazilas")]
    public async Task<IActionResult> GetUpazilas([FromQuery] int? districtId)
    {
        var query = _db.Upazilas.AsQueryable();
        if (districtId.HasValue)
        {
            query = query.Where(u => u.DistrictId == districtId.Value);
        }

        var upazilas = await query
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.DistrictId, u.Name, u.NameBn })
            .ToListAsync();
        return Ok(upazilas);
    }
}

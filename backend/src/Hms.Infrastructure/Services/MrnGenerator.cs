using Microsoft.EntityFrameworkCore;
using Hms.Application.Common.Interfaces;
using Hms.Infrastructure.Persistence;

namespace Hms.Infrastructure.Services;

public class MrnGenerator : IMrnGenerator
{
    private readonly HmsDbContext _db;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public MrnGenerator(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateMrnAsync(Guid hospitalId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var year = DateTime.UtcNow.Year;
            var yearPrefix = $"{year}-";

            // Find max MRN for this hospital and year
            var latestMrn = await _db.Patients
                .IgnoreQueryFilters()
                .Where(p => p.HospitalId == hospitalId && p.MRN.StartsWith(yearPrefix))
                .OrderByDescending(p => p.MRN)
                .Select(p => p.MRN)
                .FirstOrDefaultAsync(cancellationToken);

            var nextSequence = 1;
            if (latestMrn != null)
            {
                var parts = latestMrn.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out var seq))
                {
                    nextSequence = seq + 1;
                }
            }

            return $"{year}-{nextSequence:D6}";
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

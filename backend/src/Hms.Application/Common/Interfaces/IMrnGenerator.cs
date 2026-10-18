namespace Hms.Application.Common.Interfaces;

/// <summary>
/// Medical Record Number (MRN) generator.
/// Generates sequence-backed, unique-per-hospital identifiers formatted as YYYY-NNNNNN.
/// </summary>
public interface IMrnGenerator
{
    Task<string> GenerateMrnAsync(Guid hospitalId, CancellationToken cancellationToken = default);
}

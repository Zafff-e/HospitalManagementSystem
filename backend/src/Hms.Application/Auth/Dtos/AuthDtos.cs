namespace Hms.Application.Auth.Dtos;

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record AuthUserDto(
    Guid Id,
    string Email,
    string FullName,
    string? StaffId,
    string? BmdcRegNo,
    string? Designation,
    Guid HospitalId,
    IList<string> Roles,
    IList<string> Permissions
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    AuthUserDto User
);

public record RevokeTokenRequest(string RefreshToken);

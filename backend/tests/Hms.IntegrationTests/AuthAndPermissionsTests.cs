using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Hms.Api.Controllers;
using Hms.Application.Auth.Dtos;
using Xunit;

namespace Hms.IntegrationTests;

public class AuthAndPermissionsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthAndPermissionsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Checkpoint2_Requirement1_ValidLogin_ShouldReturnTokensAndUserPermissions()
    {
        var loginRequest = new LoginRequest("receptionist@demo-hms.local", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
        loginResult.User.Email.Should().Be("receptionist@demo-hms.local");
        loginResult.User.Roles.Should().Contain("Receptionist");
        loginResult.User.Permissions.Should().Contain("patient.read");
        loginResult.User.Permissions.Should().NotContain("billing.refund");
    }

    [Fact]
    public async Task Checkpoint2_Requirement2_WrongPasswordFiveTimes_ShouldLockAccount()
    {
        const string targetEmail = "doctor@demo-hms.local";
        const string wrongPassword = "WrongPassword999!";

        // Attempt 1 through 4: Should return 400 Bad Request
        for (int i = 1; i <= 4; i++)
        {
            var res = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(targetEmail, wrongPassword));
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // Attempt 5: Should trigger account lockout
        var fifthAttempt = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(targetEmail, wrongPassword));
        fifthAttempt.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await fifthAttempt.Content.ReadAsStringAsync();
        content.Should().Contain("locked");

        // Attempt 6 with CORRECT password: Should still be locked out!
        var lockedAttempt = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(targetEmail, "Password123!"));
        lockedAttempt.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var lockedContent = await lockedAttempt.Content.ReadAsStringAsync();
        lockedContent.Should().Contain("locked");
    }

    [Fact]
    public async Task Checkpoint2_Requirement3_ReceptionistCannotCallBillingRefund_ShouldReturn403Forbidden()
    {
        // 1. Log in as Receptionist (who lacks billing.refund permission)
        var recLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", 
            new LoginRequest("receptionist@demo-hms.local", "Password123!"));
        var recLogin = await recLoginRes.Content.ReadFromJsonAsync<LoginResponse>();

        // 2. Attempt refund call as Receptionist -> 403 Forbidden
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billing/refund")
        {
            Content = JsonContent.Create(new RefundTestRequest(Guid.NewGuid(), 500.00m, "Patient requested refund"))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", recLogin!.AccessToken);

        var recRefundResponse = await _client.SendAsync(request);
        recRefundResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Log in as BillingOfficer (who has billing.refund permission)
        var billLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", 
            new LoginRequest("billing@demo-hms.local", "Password123!"));
        var billLogin = await billLoginRes.Content.ReadFromJsonAsync<LoginResponse>();

        // 4. Attempt refund call as BillingOfficer -> 200 OK
        var billRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billing/refund")
        {
            Content = JsonContent.Create(new RefundTestRequest(Guid.NewGuid(), 500.00m, "Authorized refund"))
        };
        billRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", billLogin!.AccessToken);

        var billRefundResponse = await _client.SendAsync(billRequest);
        billRefundResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Checkpoint2_Requirement4_RefreshTokenRotation_ShouldInvalidateOldToken()
    {
        // 1. Initial Login
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", 
            new LoginRequest("billing@demo-hms.local", "Password123!"));
        var initialTokens = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();

        var oldRefreshToken = initialTokens!.RefreshToken;
        var oldAccessToken = initialTokens.AccessToken;

        // 2. Rotate Refresh Token
        var refreshRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh", 
            new RefreshTokenRequest(oldAccessToken, oldRefreshToken));
        refreshRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var newTokens = await refreshRes.Content.ReadFromJsonAsync<LoginResponse>();
        newTokens.Should().NotBeNull();
        newTokens!.RefreshToken.Should().NotBe(oldRefreshToken);
        newTokens.AccessToken.Should().NotBe(oldAccessToken);

        // 3. Attempting to reuse old refresh token must be rejected!
        var reuseRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh", 
            new RefreshTokenRequest(newTokens.AccessToken, oldRefreshToken));
        reuseRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await reuseRes.Content.ReadAsStringAsync();
        content.Should().Contain("invalid");
    }
}

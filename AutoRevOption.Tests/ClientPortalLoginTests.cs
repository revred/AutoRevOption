// ClientPortalLoginTests.cs — Tests for Client Portal automated login

using AutoRevOption.Client;
using Xunit;

namespace AutoRevOption.Tests;

/// <summary>
/// Tests for IBKR Client Portal automated login service
/// These tests use real credentials and require 2FA approval
/// </summary>
public class ClientPortalLoginTests : IDisposable
{
    private readonly ClientPortalLoginService _loginService;

    public ClientPortalLoginTests()
    {
        _loginService = new ClientPortalLoginService();
    }

    [Fact(Skip = "Requires manual 2FA approval on mobile")]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthenticatedClient()
    {
        // Arrange & Act
        var apiClient = await _loginService.LoginAsync(headless: true, twoFactorTimeoutMinutes: 2);

        // Assert
        Assert.NotNull(apiClient);

        var authStatus = await apiClient.GetAuthStatusAsync();
        Assert.NotNull(authStatus);
        Assert.True(authStatus.Authenticated);
        Assert.True(authStatus.Connected);
    }

    [Fact(Skip = "Requires manual 2FA approval on mobile")]
    public async Task LoginAsync_MultipleProcesses_SharesSameGatewaySession()
    {
        // Arrange
        var service1 = new ClientPortalLoginService();
        var service2 = new ClientPortalLoginService();

        // Act - First login (requires 2FA)
        var client1 = await service1.LoginAsync(headless: true, twoFactorTimeoutMinutes: 2);

        // Act - Second login should reuse session (no 2FA needed)
        var client2 = await service2.LoginAsync(headless: true, twoFactorTimeoutMinutes: 2);

        // Assert - Both should be authenticated
        var auth1 = await client1.GetAuthStatusAsync();
        var auth2 = await client2.GetAuthStatusAsync();

        Assert.True(auth1?.Authenticated);
        Assert.True(auth2?.Authenticated);

        // Cleanup
        service1.Dispose();
        service2.Dispose();
    }

    [Fact(Skip = "Requires manual 2FA approval on mobile")]
    public async Task GetAccountsAsync_AfterLogin_ReturnsAccounts()
    {
        // Arrange
        var apiClient = await _loginService.LoginAsync(headless: true, twoFactorTimeoutMinutes: 2);

        // Act
        var accounts = await apiClient.GetAccountsAsync();

        // Assert
        Assert.NotNull(accounts);
        Assert.NotEmpty(accounts);

        foreach (var account in accounts)
        {
            Assert.NotNull(account.AccountId);
            Console.WriteLine($"Account: {account.AccountId} - {account.DisplayName}");
        }
    }

    [Fact(Skip = "Requires manual 2FA approval on mobile")]
    public async Task GetPositionsAsync_AfterLogin_ReturnsPositions()
    {
        // Arrange
        var apiClient = await _loginService.LoginAsync(headless: true, twoFactorTimeoutMinutes: 2);
        var accounts = await apiClient.GetAccountsAsync();
        Assert.NotNull(accounts);
        Assert.NotEmpty(accounts);

        var firstAccountId = accounts[0].AccountId;

        // Act
        var positions = await apiClient.GetPositionsAsync(firstAccountId);

        // Assert
        Assert.NotNull(positions);
        // Note: positions list may be empty if no positions exist

        foreach (var position in positions)
        {
            Console.WriteLine($"Position: {position.ContractDesc} - Size: {position.PositionSize}");
        }
    }

    public void Dispose()
    {
        _loginService?.Dispose();
    }
}

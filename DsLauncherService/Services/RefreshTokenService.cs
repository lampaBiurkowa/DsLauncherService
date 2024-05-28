using DsIdentity.ApiClient;
using DsLauncherService.Models;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace DsLauncherService.Services;

internal class RefreshTokenService(DsIdentityClientFactory clientFactory) : BackgroundService
{
    private Credentials? credentials;
    private readonly TimeSpan refreshInterval = TimeSpan.FromMinutes(3);
    private readonly Stopwatch refreshStopwatch = new();
    private readonly DsIdentityClientFactory clientFactory = clientFactory;

    public void SetCredentials(Credentials credentials)
    {
        this.credentials = credentials;
        refreshStopwatch.Restart();
    }

    public string? GetToken() => credentials?.Token;
    public Guid? GetUserGuid() => credentials?.UserGuid;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (refreshStopwatch.Elapsed > refreshInterval && credentials != null)
            {
                credentials.Token = await clientFactory.CreateClient(credentials.Token).Auth_LoginAsync(credentials.UserGuid, credentials.PasswordHash);
                refreshStopwatch.Restart();
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}

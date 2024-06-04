using System.Diagnostics;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DsLauncherService.Services;

public class GameActivityService(
    DsLauncherClientFactory clientFactory,
    CacheService cache,
    IServiceProvider serviceProvider) : BackgroundService
{
    CurrentActivity? runningGame;
    readonly TimeSpan checkInterval = TimeSpan.FromSeconds(5);

    public async Task<bool> StartGame(Guid productGuid, Process process, CancellationToken ct)
    {
        if (runningGame != null) return false;

        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Storage.Activity>>();
        var localData = new Storage.Activity
        {
            ProductGuid = productGuid,
            UserGuid = cache.GetUser() ?? throw new(),
            StarDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow
        };

        await repo.InsertAsync(localData, ct);

        process.Start();
        process.Exited += OnProcessExit;

        await repo.CommitAsync(ct);

        runningGame = new()
        {
            ProductGuid = productGuid,
            Process = process,
            StartDate = DateTime.UtcNow,
            LocalDataId = localData.Id
        };

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Storage.Activity>>();
        while (!ct.IsCancellationRequested)
        {
            if (runningGame != null)
            {
                var activity = await repo.GetById(runningGame.LocalDataId, ct: ct);
                if (activity != null)
                {
                    activity.EndDate = DateTime.UtcNow;
                    await repo.UpdateAsync(activity, ct);

                    await GetClient().Activity_ReportActivityAsync(new()
                    {
                        StartDate = activity.StarDate,
                        EndDate = activity.EndDate,
                        ProductGuid = activity.ProductGuid,
                        UserGuid = activity.UserGuid,
                    }, ct);

                    await repo.CommitAsync(ct);
                }
            }

            await Task.Delay(checkInterval, ct);
        }
    }

    async void OnProcessExit(object? sender, EventArgs e)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Storage.Activity>>();
        
        if (runningGame != null)
        {
            await repo.DeleteAsync(runningGame.LocalDataId, default);
            await repo.CommitAsync(default);
            runningGame = null;
        }
    }

    DsLauncherClient GetClient() => clientFactory.CreateClient(cache.GetToken() ?? throw new());
}
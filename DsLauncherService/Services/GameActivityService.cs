using System.Diagnostics;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Models;
using Microsoft.Extensions.Hosting;

namespace DsLauncherService.Services;

public class GameActivityService(
    DsLauncherClientFactory clientFactory,
    CacheService cache,
    Repository<Storage.Activity> repo) : BackgroundService
{
    CurrentActivity? runningGame;
    readonly TimeSpan checkInterval = TimeSpan.FromSeconds(5);
    readonly SemaphoreSlim dbLock = new(1, 1);

    public async Task<bool> StartGame(Guid productGuid, Process process, CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref runningGame, null, null) != null)
            return false;

        var localData = new Storage.Activity
        {
            ProductGuid = productGuid,
            UserGuid = cache.GetUser() ?? throw new(),
            StarDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow
        };

        await repo.InsertAsync(localData, ct);

        process.Start();
        process.EnableRaisingEvents = true;
        process.Exited += OnProcessExit;

        await repo.CommitAsync(ct);

        var newRunningGame = new CurrentActivity(productGuid, process, DateTime.UtcNow, localData.Id);
        Interlocked.Exchange(ref runningGame, newRunningGame);

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var currentActivity = Interlocked.CompareExchange(ref runningGame, null, null);

            if (currentActivity != null)
            {
                await dbLock.WaitAsync(ct);
                try
                {
                    var activity = await repo.GetById(currentActivity.LocalDataId, ct: ct);
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
                finally
                {
                    dbLock.Release();
                }
            }

            await Task.Delay(checkInterval, ct);
        }
    }

    async void OnProcessExit(object? sender, EventArgs e)
    {   
        var currentActivity = Interlocked.Exchange(ref runningGame, null);
        if (currentActivity != null)
        {
            await dbLock.WaitAsync();
            try
            {
                await repo.DeleteAsync(currentActivity.LocalDataId, default);
                await repo.CommitAsync(default);
            }
            finally
            {
                dbLock.Release();
            }
        }
    }

    DsLauncherClient GetClient() => clientFactory.CreateClient(cache.GetToken() ?? throw new());
}
using BerberApp.Application.Common.Interfaces;
using BerberApp.Infrastructure.Jobs;
using Hangfire;

namespace BerberApp.Infrastructure.Services;

/// <inheritdoc />
public class HangfireBroadcastQueue : IBroadcastQueue
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireBroadcastQueue(IBackgroundJobClient jobs) => _jobs = jobs;

    public void Enqueue(Guid broadcastJobId)
        => _jobs.Enqueue<BroadcastJobRunner>(runner => runner.RunAsync(broadcastJobId));
}

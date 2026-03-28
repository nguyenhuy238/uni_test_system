using Microsoft.Extensions.DependencyInjection;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Services;

public sealed class SessionTimerBackgroundService : BackgroundService
{
    private const int DefaultDurationMinutes = 30;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionTimerBackgroundService> _logger;

    public SessionTimerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionTimerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
                var examHubNotifier = scope.ServiceProvider.GetRequiredService<IExamHubNotifier>();
                var testRepo = scope.ServiceProvider.GetRequiredService<IRepository<Test>>();

                var activeSessions = await sessionService.GetActiveSessionsForTimerAsync();
                if (activeSessions.Count > 0)
                {
                    var testIds = activeSessions.Select(s => s.TestId).Distinct().ToList();
                    var tests = testIds.Count == 0
                        ? new List<Test>()
                        : await testRepo.GetAllAsync(t => testIds.Contains(t.Id));
                    var durationByTestId = tests.ToDictionary(t => t.Id, t => Math.Max(1, t.DurationMinutes));

                    foreach (var session in activeSessions)
                    {
                        if (stoppingToken.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            var durationMinutes = durationByTestId.TryGetValue(session.TestId, out var duration)
                                ? duration
                                : DefaultDurationMinutes;
                            var remainingSeconds = ComputeRemainingSeconds(session, durationMinutes);

                            if (remainingSeconds <= 0)
                            {
                                var submitData = await sessionService.AutoSubmitAsync(session.Id);
                                if (submitData != null)
                                {
                                    await examHubNotifier.NotifyAutoSubmittedAsync(session.Id);
                                }

                                continue;
                            }

                            await examHubNotifier.PushTimerSyncAsync(session.Id, remainingSeconds, running: true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process session {SessionId} in timer tick.", session.Id);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in SessionTimerBackgroundService loop.");
            }

            try
            {
                await Task.Delay(TickInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static int ComputeRemainingSeconds(Session session, int durationMinutes)
    {
        var total = Math.Max(1, durationMinutes) * 60;
        var runningDelta = session.TimerStartedAt.HasValue
            ? (int)Math.Floor((DateTime.UtcNow - session.TimerStartedAt.Value).TotalSeconds)
            : 0;
        var consumed = Math.Max(0, session.ConsumedSeconds + Math.Max(0, runningDelta));
        return Math.Max(0, total - consumed);
    }
}

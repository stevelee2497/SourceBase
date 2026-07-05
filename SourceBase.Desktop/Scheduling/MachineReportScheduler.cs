using System.Windows.Threading;

namespace SourceBase.Desktop.Scheduling;

/// <summary>
/// Periodically reports the machine's status (Active) to the SourceBase API.
/// Runs on a 5-minute interval. Silent no-op if API is unreachable.
/// </summary>
public sealed class MachineReportScheduler(Func<Task> reportActiveAsync)
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(5) };

    public void Start()
    {
        _timer.Tick += async (_, _) =>
        {
            try
            {
                await reportActiveAsync();
            }
            catch { /* silent fail */ }
        };
        _timer.Start();
    }

    public void Stop() => _timer.Stop();
}

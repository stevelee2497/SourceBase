using System.Diagnostics;

namespace SourceBase.Desktop.Services;

/// <summary>
/// Executes power actions (shutdown, restart) on the local machine using Windows native commands.
/// Silent no-op on error (e.g., insufficient privileges).
/// </summary>
public static class PowerActionService
{
    /// <summary>Shuts down the machine cleanly.</summary>
    public static void Shutdown() => ExecutePowerCommand("/s");

    /// <summary>Restarts the machine cleanly.</summary>
    public static void Restart() => ExecutePowerCommand("/r");

    private static void ExecutePowerCommand(string command)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = command,
                CreateNoWindow = true,
                UseShellExecute = false,
            });
        }
        catch { /* silent fail */ }
    }
}

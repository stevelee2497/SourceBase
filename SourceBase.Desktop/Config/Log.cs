using System.IO;

namespace SourceBase.Desktop.Config;

/// <summary>
/// Minimal append-only diagnostic log written to <c>%AppData%\Jupiter\logs\jupiter.log</c>,
/// for investigating field issues (e.g. why API sync / test connection fails after an upgrade)
/// where no debugger is attached. Silent-failing per desktop conventions — a logging failure
/// must never crash the tray app. Rotates once the file passes ~1&#160;MB by moving it to
/// <c>jupiter.log.1</c> (single previous generation kept).
/// </summary>
internal static class Log
{
    private const long MaxBytes = 1_000_000;
    private static readonly object Gate = new();

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jupiter", "logs");

    private static readonly string FilePath = Path.Combine(Dir, "jupiter.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message} :: {ex.GetType().Name}: {ex.Message}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Dir);
                Rotate();
                File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch { /* diagnostics only — never break the app because logging failed */ }
    }

    private static void Rotate()
    {
        var info = new FileInfo(FilePath);
        if (!info.Exists || info.Length < MaxBytes) return;
        var prev = FilePath + ".1";
        if (File.Exists(prev)) File.Delete(prev);
        File.Move(FilePath, prev);
    }
}

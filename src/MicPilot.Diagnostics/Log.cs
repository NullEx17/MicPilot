using System.Text;

namespace MicPilot.Diagnostics;

public static class Log
{
    private static readonly object Sync = new();
    private static string? _logFilePath;

    public static string LogDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MicPilot",
            "logs");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null)
    {
        if (exception is null)
        {
            Write("ERROR", message);
            return;
        }

        Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    }

    public static string GetDiagnosticsText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("MicPilot Diagnostics");
        builder.AppendLine($"Version: {GetAppVersion()}");
        builder.AppendLine($"OS: {Environment.OSVersion}");
        builder.AppendLine($"64-bit process: {Environment.Is64BitProcess}");
        builder.AppendLine($"Log file: {GetLogFilePath()}");
        builder.AppendLine($"Settings: {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MicPilot", "settings.json")}");

        try
        {
            var logPath = GetLogFilePath();
            if (File.Exists(logPath))
            {
                builder.AppendLine();
                builder.AppendLine("--- Recent log ---");
                var lines = File.ReadAllLines(logPath);
                var tail = lines.Length <= 80 ? lines : lines[^80..];
                builder.AppendLine(string.Join(Environment.NewLine, tail));
            }
        }
        catch (Exception ex)
        {
            builder.AppendLine($"Failed to read log tail: {ex.Message}");
        }

        return builder.ToString();
    }

    private static string GetAppVersion() =>
        typeof(Log).Assembly.GetName().Version?.ToString() ?? "0.1.0";

    private static string GetLogFilePath()
    {
        if (_logFilePath is not null)
        {
            return _logFilePath;
        }

        Directory.CreateDirectory(LogDirectory);
        _logFilePath = Path.Combine(LogDirectory, $"micpilot-{DateTime.Now:yyyy-MM-dd}.log");
        return _logFilePath;
    }

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

        lock (Sync)
        {
            try
            {
                File.AppendAllText(GetLogFilePath(), line + Environment.NewLine);
            }
            catch
            {
                // Logging must never crash the app.
            }
        }
    }
}

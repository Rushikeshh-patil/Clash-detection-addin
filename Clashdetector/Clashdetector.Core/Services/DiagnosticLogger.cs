using System.Text;

namespace Clashdetector.Core.Services;

public sealed class DiagnosticLogger
{
    private readonly string _logDirectoryPath;
    private readonly object _sync = new();

    public DiagnosticLogger(string? logDirectoryPath = null)
    {
        _logDirectoryPath = string.IsNullOrWhiteSpace(logDirectoryPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClashDetector",
                "logs")
            : logDirectoryPath;
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warn(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        var finalMessage = exception is null
            ? message
            : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", finalMessage);
    }

    private void Write(string level, string message)
    {
        try
        {
            lock (_sync)
            {
                Directory.CreateDirectory(_logDirectoryPath);
                var filePath = Path.Combine(_logDirectoryPath, $"{DateTime.UtcNow:yyyyMMdd}.log");
                var line = $"{DateTimeOffset.UtcNow:O} [{level}] {message}";
                File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must not break host execution.
        }
    }
}

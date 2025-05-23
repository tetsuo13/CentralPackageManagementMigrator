using Microsoft.Extensions.Logging;

namespace CentralPackageManagementMigrator;

/// <summary>
/// Manual logging management utility.
/// </summary>
internal static class LoggingUtility
{
    private static ILoggerFactory? _loggerFactory;
    private static ILoggerFactory Factory
    {
        get => _loggerFactory ?? throw new InvalidOperationException($"Call {nameof(SetupLogging)} first");
        set => _loggerFactory = value;
    }

    public static void SetupLogging(LogLevel logLevel)
    {
        Factory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });

            builder.SetMinimumLevel(logLevel);
        });
    }

    /// <summary>
    /// Manually call after all logging statements in order to flush any
    /// existing logs that haven't been output yet. Failure to call this
    /// method may result in lost messages.
    /// </summary>
    public static void FlushLogging() => Factory?.Dispose();

    public static ILogger<T> CreateLogger<T>() => Factory.CreateLogger<T>();
}

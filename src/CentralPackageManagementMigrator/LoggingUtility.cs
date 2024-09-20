using Microsoft.Extensions.Logging;

namespace CentralPackageManagementMigrator;

internal static class LoggingUtility
{
    private static ILoggerFactory? _loggerFactory;
    private static ILoggerFactory Factory
    {
        get => _loggerFactory ?? throw new NullReferenceException($"Call {nameof(SetupLogging)} first");
        set => _loggerFactory = value;
    }

    public static void SetupLogging(LogLevel logLevel)
    {
        Factory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                // TODO: Disable scopes or something
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });

            builder.SetMinimumLevel(logLevel);
        });
    }

    public static void FlushLogging() => Factory?.Dispose();

    public static ILogger<T> CreateLogger<T>() => Factory.CreateLogger<T>();
}

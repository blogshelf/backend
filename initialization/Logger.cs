namespace backend.initialization;

public abstract class Logger
{
    public static ILoggerFactory LogFactory { get; } = LoggerFactory.Create(builder => builder.AddConsole());
}
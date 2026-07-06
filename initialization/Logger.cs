
namespace backend.initialization;

public class Logger
{
    public static ILoggerFactory LogFactory { get; } = LoggerFactory.Create(builder=> builder.AddConsole());
}
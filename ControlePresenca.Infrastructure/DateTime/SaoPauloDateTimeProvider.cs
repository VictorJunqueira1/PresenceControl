using ControlePresenca.Application.Contracts;

namespace ControlePresenca.Infrastructure.DateTime;

public sealed class SaoPauloDateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo _timeZone = ResolveTimeZone();

    public DateTimeOffset GetNow()
    {
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _timeZone);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
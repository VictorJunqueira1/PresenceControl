namespace ControlePresenca.Application.Contracts;

public interface IDateTimeProvider
{
    DateTimeOffset GetNow();
}
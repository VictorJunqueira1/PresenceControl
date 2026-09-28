namespace ControlePresenca.Domain.Services;

public static class StudentNameNormalizer
{
    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var parts = name
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(" ", parts);
    }
}
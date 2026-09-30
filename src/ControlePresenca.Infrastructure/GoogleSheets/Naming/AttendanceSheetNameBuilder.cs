using ControlePresenca.Domain.Entities;
using System.Globalization;

namespace ControlePresenca.Infrastructure.GoogleSheets.Naming;

internal static class AttendanceSheetNameBuilder
{
    private const int MaxSheetNameLength = 100;
    private static readonly char[] InvalidCharacters = ['[', ']', '*', '?', '/', '\\', ':'];

    public static string Build(Attendance attendance)
    {
        var date = attendance.RegisteredAt.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        var suffix = $" - {date}";
        var activityName = Sanitize(attendance.ActivityName);
        var maxActivityNameLength = MaxSheetNameLength - suffix.Length;

        if (activityName.Length > maxActivityNameLength)
            activityName = activityName[..maxActivityNameLength].TrimEnd();

        return $"{activityName}{suffix}";
    }

    private static string Sanitize(string activityName)
    {
        var sanitized = new string(activityName
            .Trim()
            .Select(character => InvalidCharacters.Contains(character) ? '-' : character)
            .ToArray());

        sanitized = string.Join(' ', sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim('\'');

        return string.IsNullOrWhiteSpace(sanitized) ? "Atividade" : sanitized;
    }
}
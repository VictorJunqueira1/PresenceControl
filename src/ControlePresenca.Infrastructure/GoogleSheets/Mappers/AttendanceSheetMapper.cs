using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.Enums;
using ControlePresenca.Domain.Services;
using System.Globalization;

namespace ControlePresenca.Infrastructure.GoogleSheets.Mappers;

internal static class AttendanceSheetMapper
{
    public static IList<object> ToRow(Attendance attendance)
        => new List<object>
        {
            attendance.RegisteredAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            attendance.RegisteredAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            attendance.ActivityId,
            attendance.ActivityName,
            StudentNameNormalizer.Normalize(attendance.StudentName),
            NormalizeRA(attendance.RA),
            GetAttendanceTypeDescription(attendance.Type),
            attendance.DeviceId
        };

    public static string NormalizeRA(string ra) => ra.Trim();

    public static string GetAttendanceTypeDescription(AttendanceType type)
        => type switch
        {
            AttendanceType.Entry => "Entrada",
            AttendanceType.Exit => "Saída",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    public static bool TryParseAttendanceType(string value, out AttendanceType type)
    {
        if (value.Equals("Entrada", StringComparison.OrdinalIgnoreCase))
        {
            type = AttendanceType.Entry;
            return true;
        }

        if (value.Equals("Saída", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Saida", StringComparison.OrdinalIgnoreCase))
        {
            type = AttendanceType.Exit;
            return true;
        }

        type = default;
        return false;
    }
}

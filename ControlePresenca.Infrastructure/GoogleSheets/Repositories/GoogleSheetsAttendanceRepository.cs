using ControlePresenca.Application.Contracts;
using ControlePresenca.Application.Enums;
using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.Enums;
using ControlePresenca.Domain.Services;
using Google;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;

namespace ControlePresenca.Infrastructure.GoogleSheets.Repositories;

public sealed class GoogleSheetsAttendanceRepository : IAttendanceRepository
{
    private readonly GoogleSheetsClient _client;
    private readonly ILogger<GoogleSheetsAttendanceRepository> _logger;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sheetLocks = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _registeredKeys = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _registeredDeviceKeys = new();

    public GoogleSheetsAttendanceRepository(
        GoogleSheetsClient client,
        ILogger<GoogleSheetsAttendanceRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<AttendancePersistenceStatus> TryRegisterAsync(
        Attendance attendance,
        CancellationToken cancellationToken = default)
    {
        var sheetName = BuildSheetName(attendance);
        var sheetLock = _sheetLocks.GetOrAdd(sheetName, _ => new SemaphoreSlim(1, 1));

        await sheetLock.WaitAsync(cancellationToken);

        try
        {
            await EnsureSheetInitializedAsync(sheetName, cancellationToken);

            var ra = NormalizeRA(attendance.RA);

            var studentKey = BuildStudentKey(
                attendance.ActivityId,
                ra,
                attendance.Type);

            var deviceKey = BuildDeviceKey(
                attendance.ActivityId,
                attendance.DeviceId,
                attendance.Type);

            var registeredKeys = _registeredKeys[sheetName];
            var registeredDeviceKeys = _registeredDeviceKeys[sheetName];

            if (registeredKeys.Contains(studentKey))
                return AttendancePersistenceStatus.Duplicate;

            if (registeredDeviceKeys.Contains(deviceKey))
                return AttendancePersistenceStatus.DeviceAlreadyUsed;

            await _client.AppendRowAsync(
                sheetName,
                new List<object>
                {
                    attendance.RegisteredAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    attendance.RegisteredAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                    attendance.ActivityId,
                    attendance.ActivityName,
                    StudentNameNormalizer.Normalize(attendance.StudentName),
                    ra,
                    GetAttendanceTypeDescription(attendance.Type),
                    attendance.DeviceId
                },
                cancellationToken);

            registeredKeys.Add(studentKey);
            registeredDeviceKeys.Add(deviceKey);

            return AttendancePersistenceStatus.Registered;
        }
        catch (GoogleApiException ex)
        {
            _logger.LogError(ex, "Erro ao registrar presença no Google Sheets.");
            return AttendancePersistenceStatus.Unavailable;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de comunicação ao registrar presença no Google Sheets.");
            return AttendancePersistenceStatus.Unavailable;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout ao registrar presença no Google Sheets.");
            return AttendancePersistenceStatus.Unavailable;
        }
        finally
        {
            sheetLock.Release();
        }
    }

    private async Task EnsureSheetInitializedAsync(
        string sheetName,
        CancellationToken cancellationToken)
    {
        if (_registeredKeys.ContainsKey(sheetName)
            && _registeredDeviceKeys.ContainsKey(sheetName))
            return;

        var exists = await _client.SheetExistsAsync(sheetName, cancellationToken);

        if (!exists)
        {
            await _client.CreateSheetAsync(sheetName, cancellationToken);

            await _client.AppendRowAsync(
                sheetName,
                new List<object>
                {
                    "Data",
                    "Hora",
                    "ActivityId",
                    "Atividade",
                    "Nome",
                    "RA",
                    "TipoRegistro",
                    "DeviceId"
                },
                cancellationToken);
        }

        var quotedSheet = GoogleSheetsClient.QuoteSheetName(sheetName);

        var rows = await _client.ReadAsync(
            $"{quotedSheet}!C2:H",
            cancellationToken);

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deviceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (row.Count < 5)
                continue;

            var activityId = row[0]?.ToString()?.Trim();
            var ra = row[3]?.ToString()?.Trim();
            var typeText = row[4]?.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(activityId)
                || string.IsNullOrWhiteSpace(ra)
                || string.IsNullOrWhiteSpace(typeText))
                continue;

            if (!TryParseAttendanceType(typeText, out var type))
                continue;

            keys.Add(BuildStudentKey(
                activityId,
                ra,
                type));

            if (row.Count >= 6)
            {
                var deviceId = row[5]?.ToString()?.Trim();

                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    deviceKeys.Add(BuildDeviceKey(
                        activityId,
                        deviceId,
                        type));
                }
            }
        }

        _registeredKeys[sheetName] = keys;
        _registeredDeviceKeys[sheetName] = deviceKeys;
    }

    private static string BuildSheetName(Attendance attendance)
    {
        return attendance.RegisteredAt.ToString(
            "dd-MM-yyyy",
            CultureInfo.InvariantCulture);
    }

    private static string BuildStudentKey(
        string activityId,
        string ra,
        AttendanceType type)
    {
        return $"{activityId.Trim()}|{NormalizeRA(ra)}|{type}";
    }

    private static string BuildDeviceKey(
        string activityId,
        string deviceId,
        AttendanceType type)
    {
        return $"{activityId.Trim()}|{deviceId.Trim()}|{type}";
    }

    private static string NormalizeRA(string ra)
    {
        return ra.Trim();
    }

    private static string GetAttendanceTypeDescription(AttendanceType type)
    {
        return type switch
        {
            AttendanceType.Entry => "Entrada",
            AttendanceType.Exit => "Saída",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static bool TryParseAttendanceType(
        string value,
        out AttendanceType type)
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
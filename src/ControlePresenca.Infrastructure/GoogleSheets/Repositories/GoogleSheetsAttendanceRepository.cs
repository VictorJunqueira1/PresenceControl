using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.Enums;
using ControlePresenca.Infrastructure.GoogleSheets.Mappers;
using ControlePresenca.Infrastructure.GoogleSheets.Naming;
using Google;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ControlePresenca.Infrastructure.GoogleSheets.Repositories;

public sealed class GoogleSheetsAttendanceRepository(
    IGoogleSheetsClient client,
    ILogger<GoogleSheetsAttendanceRepository> logger)
    : IAttendanceRepository
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sheetLocks = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _registeredKeys = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _registeredDeviceKeys = new();

    public async Task<AttendancePersistenceStatus> TryRegisterAsync(
        Attendance attendance,
        CancellationToken cancellationToken = default)
    {
        var sheetName = AttendanceSheetNameBuilder.Build(attendance);
        var sheetLock = _sheetLocks.GetOrAdd(sheetName, _ => new SemaphoreSlim(1, 1));

        await sheetLock.WaitAsync(cancellationToken);

        try
        {
            await EnsureSheetInitializedAsync(sheetName, cancellationToken);

            var ra = AttendanceSheetMapper.NormalizeRA(attendance.RA);
            var studentKey = BuildStudentKey(attendance.ActivityId, ra, attendance.Type);
            var deviceKey = BuildDeviceKey(attendance.ActivityId, attendance.DeviceId, attendance.Type);
            var registeredKeys = _registeredKeys[sheetName];
            var registeredDeviceKeys = _registeredDeviceKeys[sheetName];

            if (registeredKeys.Contains(studentKey))
                return AttendancePersistenceStatus.Duplicate;

            if (registeredDeviceKeys.Contains(deviceKey))
                return AttendancePersistenceStatus.DeviceAlreadyUsed;

            await client.AppendRowAsync(
                sheetName,
                AttendanceSheetMapper.ToRow(attendance),
                cancellationToken);

            registeredKeys.Add(studentKey);
            registeredDeviceKeys.Add(deviceKey);

            return AttendancePersistenceStatus.Registered;
        }
        catch (GoogleApiException ex)
        {
            InvalidateCache(sheetName);
            logger.LogError(ex, "Erro ao registrar presença no Google Sheets.");
            return AttendancePersistenceStatus.Unavailable;
        }
        catch (HttpRequestException ex)
        {
            InvalidateCache(sheetName);
            logger.LogError(ex, "Erro de comunicação ao registrar presença no Google Sheets.");
            return AttendancePersistenceStatus.Unavailable;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            InvalidateCache(sheetName);
            logger.LogError(ex, "Timeout ao registrar presença no Google Sheets.");
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

        var exists = await client.SheetExistsAsync(sheetName, cancellationToken);

        if (!exists)
        {
            await client.CreateSheetAsync(sheetName, cancellationToken);
            await client.AppendRowAsync(
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
        var rows = await client.ReadAsync($"{quotedSheet}!C2:H", cancellationToken);
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
                || string.IsNullOrWhiteSpace(typeText)
                || !AttendanceSheetMapper.TryParseAttendanceType(typeText, out var type))
                continue;

            keys.Add(BuildStudentKey(activityId, ra, type));

            if (row.Count < 6)
                continue;

            var deviceId = row[5]?.ToString()?.Trim();

            if (!string.IsNullOrWhiteSpace(deviceId))
                deviceKeys.Add(BuildDeviceKey(activityId, deviceId, type));
        }

        _registeredKeys[sheetName] = keys;
        _registeredDeviceKeys[sheetName] = deviceKeys;
    }

    private void InvalidateCache(string sheetName)
    {
        _registeredKeys.TryRemove(sheetName, out _);
        _registeredDeviceKeys.TryRemove(sheetName, out _);
    }

    private static string BuildStudentKey(
        string activityId,
        string ra,
        AttendanceType type)
        => $"{activityId.Trim()}|{AttendanceSheetMapper.NormalizeRA(ra)}|{type}";

    private static string BuildDeviceKey(
        string activityId,
        string deviceId,
        AttendanceType type)
        => $"{activityId.Trim()}|{deviceId.Trim()}|{type}";
}

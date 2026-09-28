using ControlePresenca.Application.Contracts;
using ControlePresenca.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControlePresenca.Infrastructure.Pending;

public sealed class JsonPendingAttendanceStore : IPendingAttendanceStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonPendingAttendanceStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task SaveAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var attendances = await ReadInternalAsync(cancellationToken);

            var alreadyExists = attendances.Any(existing => IsSameAttendance(existing, attendance));

            if (alreadyExists)
                return;

            attendances.Add(attendance);

            await WriteInternalAsync(attendances, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyCollection<Attendance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            return await ReadInternalAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var attendances = await ReadInternalAsync(cancellationToken);

            attendances.RemoveAll(existing =>
                IsSameAttendance(existing, attendance));

            await WriteInternalAsync(
                attendances,
                cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Attendance>> ReadInternalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return new List<Attendance>();

        await using var stream = File.OpenRead(_filePath);

        return await JsonSerializer.DeserializeAsync<List<Attendance>>(
                   stream,
                   _jsonOptions,
                   cancellationToken)
               ?? new List<Attendance>();
    }

    private async Task WriteInternalAsync(List<Attendance> attendances, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempFile = $"{_filePath}.tmp";

        await using (var stream = File.Create(tempFile))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                attendances,
                _jsonOptions,
                cancellationToken);
        }

        File.Move(tempFile, _filePath, true);
    }

    private static bool IsSameAttendance(Attendance first, Attendance second)
    {
        return string.Equals(
                   first.ActivityId,
                   second.ActivityId,
                   StringComparison.OrdinalIgnoreCase)
               && string.Equals(first.RA, second.RA, StringComparison.OrdinalIgnoreCase)
               && first.Type == second.Type;
    }
}
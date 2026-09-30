using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Application.Contracts;
using ControlePresenca.Domain.Entities;
using ControlePresenca.Infrastructure.GoogleSheets;
using System.Collections.Concurrent;

namespace ControlePresenca.Tests.Support;

internal sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
{
    public DateTimeOffset GetNow() => now;
}

internal sealed class StubActivityRepository(Activity? activity) : IActivityRepository
{
    public Task<Activity?> GetByIdAsync(
        string activityId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(activity);
}

internal sealed class StubAttendanceRepository(Func<Attendance, AttendancePersistenceStatus> resultFactory) : IAttendanceRepository
{
    public Attendance? LastAttendance { get; private set; }

    public Task<AttendancePersistenceStatus> TryRegisterAsync(
        Attendance attendance,
        CancellationToken cancellationToken = default)
    {
        LastAttendance = attendance;
        return Task.FromResult(resultFactory(attendance));
    }
}

internal sealed class InMemoryPendingAttendanceStore : IPendingAttendanceStore
{
    private readonly List<Attendance> _items = new();
    private readonly object _sync = new();

    public bool ThrowOnSave { get; set; }

    public Task SaveAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSave)
            throw new IOException("Falha simulada ao persistir pendência.");

        lock (_sync)
        {
            if (!_items.Any(item => IsSame(item, attendance)))
                _items.Add(attendance);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Attendance>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
            return Task.FromResult<IReadOnlyCollection<Attendance>>(_items.ToArray());
    }

    public Task RemoveAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        lock (_sync)
            _items.RemoveAll(item => IsSame(item, attendance));

        return Task.CompletedTask;
    }

    private static bool IsSame(Attendance first, Attendance second)
        => string.Equals(first.ActivityId, second.ActivityId, StringComparison.OrdinalIgnoreCase)
           && string.Equals(first.RA, second.RA, StringComparison.OrdinalIgnoreCase)
           && first.Type == second.Type;
}

internal sealed class InMemoryGoogleSheetsClient : IGoogleSheetsClient
{
    private readonly ConcurrentDictionary<string, List<IList<object>>> _sheets =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public bool FailAfterSuccessfulDataAppendOnce { get; set; }

    public int DataRowCount
    {
        get
        {
            lock (_sync)
                return _sheets.Values.Sum(rows => rows.Count(row => !IsHeader(row)));
        }
    }

    public Task<IList<IList<object>>> ReadAsync(
        string range,
        CancellationToken cancellationToken = default)
    {
        var sheetName = ExtractSheetName(range);

        lock (_sync)
        {
            if (!_sheets.TryGetValue(sheetName, out var rows))
                return Task.FromResult<IList<IList<object>>>(new List<IList<object>>());

            if (range.Contains("!C2:H", StringComparison.OrdinalIgnoreCase))
            {
                var selected = rows
                    .Where(row => !IsHeader(row))
                    .Select(row => (IList<object>)row.Skip(2).Take(6).ToList())
                    .ToList();

                return Task.FromResult<IList<IList<object>>>(selected);
            }

            return Task.FromResult<IList<IList<object>>>(
                rows.Select(row => (IList<object>)row.ToList()).ToList());
        }
    }

    public Task AppendRowAsync(
        string sheetName,
        IList<object> values,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var rows = _sheets.GetOrAdd(sheetName, _ => new List<IList<object>>());
            rows.Add(values.ToList());

            if (FailAfterSuccessfulDataAppendOnce && !IsHeader(values))
            {
                FailAfterSuccessfulDataAppendOnce = false;
                throw new HttpRequestException("Falha simulada após confirmação remota.");
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> SheetExistsAsync(
        string sheetName,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_sheets.ContainsKey(sheetName));

    public Task CreateSheetAsync(
        string sheetName,
        CancellationToken cancellationToken = default)
    {
        _sheets.TryAdd(sheetName, new List<IList<object>>());
        return Task.CompletedTask;
    }

    private static bool IsHeader(IList<object> row)
        => row.Count > 0
           && string.Equals(row[0]?.ToString(), "Data", StringComparison.OrdinalIgnoreCase);

    private static string ExtractSheetName(string range)
    {
        var separator = range.IndexOf('!');
        var sheet = separator >= 0 ? range[..separator] : range;
        return sheet.Trim().Trim('\'').Replace("''", "'");
    }
}

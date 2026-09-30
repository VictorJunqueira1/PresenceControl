using ControlePresenca.Application.Activities.Models;
using ControlePresenca.Application.Activities.Queries;
using ControlePresenca.Application.Contracts;
using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.ValueObjects;
using Google;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace ControlePresenca.Infrastructure.GoogleSheets.Repositories;

public sealed class GoogleSheetsActivityRepository(
    IGoogleSheetsClient client,
    IOptions<GoogleSheetsOptions> options,
    ILogger<GoogleSheetsActivityRepository> logger)
    : IActivityRepository, IActivityQueryRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly GoogleSheetsOptions _options = options.Value;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly Dictionary<string, Activity> _cache = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _lastRefresh = DateTimeOffset.MinValue;

    public async Task<Activity?> GetByIdAsync(
        string activityId,
        CancellationToken cancellationToken = default)
    {
        await _cacheLock.WaitAsync(cancellationToken);

        try
        {
            if (DateTimeOffset.UtcNow - _lastRefresh < CacheDuration)
                return _cache.GetValueOrDefault(activityId);

            try
            {
                await RefreshCacheAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is GoogleApiException or HttpRequestException)
            {
                if (_cache.TryGetValue(activityId, out var cachedActivity))
                {
                    logger.LogWarning(
                        ex,
                        "Não foi possível atualizar as atividades. Utilizando cache para {ActivityId}.",
                        activityId);

                    return cachedActivity;
                }

                throw;
            }

            return _cache.GetValueOrDefault(activityId);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    async Task<ActivityModel?> IActivityQueryRepository.GetByIdAsync(
        string activityId,
        CancellationToken cancellationToken)
    {
        var activity = await GetByIdAsync(activityId, cancellationToken);

        return activity is null
            ? null
            : new ActivityModel(
                activity.Id,
                activity.Name,
                activity.Date,
                activity.EntryWindow.StartAt,
                activity.EntryWindow.EndAt,
                activity.ExitWindow.StartAt,
                activity.ExitWindow.EndAt);
    }

    private async Task RefreshCacheAsync(CancellationToken cancellationToken)
    {
        var sheet = GoogleSheetsClient.QuoteSheetName(_options.ActivitiesSheetName);
        var rows = await client.ReadAsync($"{sheet}!A2:G", cancellationToken);
        var activities = new Dictionary<string, Activity>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (row.Count < 7)
                continue;

            var id = GetValue(row, 0);
            var name = GetValue(row, 1);
            var dateText = GetValue(row, 2);
            var entryStartText = GetValue(row, 3);
            var entryEndText = GetValue(row, 4);
            var exitStartText = GetValue(row, 5);
            var exitEndText = GetValue(row, 6);

            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (!TryParseActivity(
                    id,
                    name,
                    dateText,
                    entryStartText,
                    entryEndText,
                    exitStartText,
                    exitEndText,
                    out var activity))
            {
                logger.LogWarning("Atividade {ActivityId} possui configuração inválida no Google Sheets.", id);
                continue;
            }

            if (!activities.TryAdd(id, activity!))
            {
                logger.LogWarning("ActivityId duplicado encontrado na aba Activities: {ActivityId}.", id);
            }
        }

        _cache.Clear();

        foreach (var activity in activities)
            _cache[activity.Key] = activity.Value;

        _lastRefresh = DateTimeOffset.UtcNow;
    }

    private static bool TryParseActivity(
        string id,
        string name,
        string dateText,
        string entryStartText,
        string entryEndText,
        string exitStartText,
        string exitEndText,
        out Activity? activity)
    {
        activity = null;

        if (!DateOnly.TryParseExact(
                dateText,
                new[] { "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            return false;

        if (!TryParseTime(entryStartText, out var entryStart)
            || !TryParseTime(entryEndText, out var entryEnd)
            || !TryParseTime(exitStartText, out var exitStart)
            || !TryParseTime(exitEndText, out var exitEnd))
            return false;

        try
        {
            activity = new Activity(
                id,
                name,
                date,
                new AttendanceWindow(entryStart, entryEnd),
                new AttendanceWindow(exitStart, exitEnd));

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryParseTime(string value, out TimeOnly time)
        => TimeOnly.TryParseExact(
            value,
            new[] { "HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);

    private static string GetValue(IList<object> row, int index)
        => index < row.Count
            ? row[index]?.ToString()?.Trim() ?? string.Empty
            : string.Empty;
}

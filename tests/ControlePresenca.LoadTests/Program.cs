using ControlePresenca.Application.Attendances.Commands.RegisterAttendance;
using ControlePresenca.Application.Attendances.Commands.ReprocessPendingAttendances;
using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Application.Contracts;
using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.Enums;
using ControlePresenca.Domain.ValueObjects;
using ControlePresenca.Infrastructure.GoogleSheets;
using ControlePresenca.Infrastructure.GoogleSheets.Repositories;
using ControlePresenca.Infrastructure.Pending;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;
using DomainActivity = ControlePresenca.Domain.Entities.Activity;

namespace ControlePresenca.LoadTests;

internal static class Program
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 9, 30, 8, 30, 0, TimeSpan.FromHours(-3));

    public static async Task<int> Main(string[] args)
    {
        var operations = ParseOperations(args);
        Console.WriteLine($"ControlePresenca - simulação de carga ({operations} operações por cenário)");
        Console.WriteLine();

        try
        {
            await RunScenarioAsync("Alunos diferentes simultâneos", operations, () => UniqueStudentsAsync(operations));
            await RunScenarioAsync("Mesmo aluno simultaneamente", operations, () => SameStudentAsync(operations));
            await RunScenarioAsync("Mesmo dispositivo simultaneamente", operations, () => SameDeviceAsync(operations));
            await RunScenarioAsync("Indisponibilidade, restart e drenagem da fila", Math.Min(operations, 250), () => RecoveryAfterRestartAsync(Math.Min(operations, 250)));
            await RunScenarioAsync("Duplicidade detectada durante reprocessamento", 1, DuplicateDuringReprocessingAsync);
            await RunScenarioAsync("Append remoto confirmado com resposta perdida", 1, AmbiguousAppendAsync);

            Console.WriteLine();
            Console.WriteLine("Todos os cenários concluídos sem violar as invariantes esperadas.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"Falha na simulação: {ex.Message}");
            return 1;
        }
    }

    private static async Task RunScenarioAsync(
        string name,
        int operations,
        Func<Task> scenario)
    {
        var stopwatch = Stopwatch.StartNew();
        await scenario();
        stopwatch.Stop();

        var throughput = operations / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
        Console.WriteLine($"{name}: {stopwatch.ElapsedMilliseconds} ms | {throughput:N2} ops/s");
    }

    private static async Task UniqueStudentsAsync(int operations)
    {
        var client = new InMemoryGoogleSheetsClient();
        var repository = CreateRepository(client);

        var tasks = Enumerable.Range(0, operations)
            .Select(index => repository.TryRegisterAsync(
                CreateAttendance($"{100000 + index}", $"device-{index}")))
            .ToArray();

        var statuses = await Task.WhenAll(tasks);

        Require(
            statuses.All(status => status == AttendancePersistenceStatus.Registered),
            "Nem todos os alunos diferentes foram registrados.");

        Require(client.DataRowCount == operations, "Quantidade final de linhas diferente do esperado.");
    }

    private static async Task SameStudentAsync(int operations)
    {
        var client = new InMemoryGoogleSheetsClient();
        var repository = CreateRepository(client);

        var tasks = Enumerable.Range(0, operations)
            .Select(index => repository.TryRegisterAsync(CreateAttendance("123456", $"device-{index}")))
            .ToArray();

        var statuses = await Task.WhenAll(tasks);

        Require(
            statuses.Count(status => status == AttendancePersistenceStatus.Registered) == 1,
            "O mesmo aluno foi registrado mais de uma vez.");

        Require(
            statuses.Count(status => status == AttendancePersistenceStatus.Duplicate) == operations - 1,
            "As duplicidades de aluno não foram identificadas corretamente.");

        Require(client.DataRowCount == 1, "O repositório gravou duplicidade para o mesmo aluno.");
    }

    private static async Task SameDeviceAsync(int operations)
    {
        var client = new InMemoryGoogleSheetsClient();
        var repository = CreateRepository(client);

        var tasks = Enumerable.Range(0, operations)
            .Select(index => repository.TryRegisterAsync(CreateAttendance($"{200000 + index}", "same-device")))
            .ToArray();

        var statuses = await Task.WhenAll(tasks);

        Require(
            statuses.Count(status => status == AttendancePersistenceStatus.Registered) == 1,
            "O mesmo dispositivo gerou mais de um registro.");

        Require(
            statuses.Count(status => status == AttendancePersistenceStatus.DeviceAlreadyUsed) == operations - 1,
            "Os conflitos de dispositivo não foram identificados corretamente.");

        Require(client.DataRowCount == 1, "O repositório gravou mais de uma linha para o mesmo dispositivo.");
    }

    private static async Task RecoveryAfterRestartAsync(int operations)
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"controle-presenca-load-{Guid.NewGuid():N}");
        var pendingPath = Path.Combine(tempDirectory, "pending-attendances.json");

        try
        {
            var client = new InMemoryGoogleSheetsClient { Available = false };
            var attendanceRepository = CreateRepository(client);
            var pendingStore = new JsonPendingAttendanceStore(pendingPath);
            var handler = new RegisterAttendanceCommandHandler(
                new FixedActivityRepository(CreateActivity()),
                attendanceRepository,
                pendingStore,
                new FixedDateTimeProvider(RegisteredAt));

            var registrationTasks = Enumerable.Range(0, operations)
                .Select(index => handler.Handle(new RegisterAttendanceCommand(
                    "atividade-1",
                    $"Aluno {index}",
                    $"{300000 + index}",
                    $"recovery-device-{index}")))
                .ToArray();

            var responses = await Task.WhenAll(registrationTasks);

            Require(
                responses.All(response => response.Status == RegisterAttendanceStatus.StoredForRetry),
                "Nem todas as presenças indisponíveis foram armazenadas para retry.");

            var beforeRestart = await pendingStore.GetAllAsync();
            Require(beforeRestart.Count == operations, "A fila pendente não acumulou todos os registros.");

            client.Available = true;

            var recreatedStore = new JsonPendingAttendanceStore(pendingPath);
            var recreatedRepository = CreateRepository(client);
            var reprocessor = new ReprocessPendingAttendancesCommandHandler(
                recreatedRepository,
                recreatedStore);

            var result = await reprocessor.Handle(new ReprocessPendingAttendancesCommand());
            var remaining = await recreatedStore.GetAllAsync();

            Require(result.Registered == operations, "A fila não foi drenada integralmente após a recuperação.");
            Require(result.Remaining == 0 && remaining.Count == 0, "Restaram registros pendentes após a recuperação.");
            Require(client.DataRowCount == operations, "A quantidade final de registros remotos está incorreta.");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }

    private static async Task DuplicateDuringReprocessingAsync()
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"controle-presenca-duplicate-{Guid.NewGuid():N}");
        var pendingPath = Path.Combine(tempDirectory, "pending-attendances.json");

        try
        {
            var client = new InMemoryGoogleSheetsClient();
            var repository = CreateRepository(client);
            var attendance = CreateAttendance("888888", "duplicate-device");

            var initial = await repository.TryRegisterAsync(attendance);
            Require(initial == AttendancePersistenceStatus.Registered, "Não foi possível preparar o registro remoto.");

            var pendingStore = new JsonPendingAttendanceStore(pendingPath);
            await pendingStore.SaveAsync(attendance);

            var reprocessor = new ReprocessPendingAttendancesCommandHandler(repository, pendingStore);
            var result = await reprocessor.Handle(new ReprocessPendingAttendancesCommand());
            var remaining = await pendingStore.GetAllAsync();

            Require(result.AlreadyRegistered == 1, "A duplicidade não foi reconhecida durante o reprocessamento.");
            Require(result.Remaining == 0 && remaining.Count == 0, "A duplicidade permaneceu indevidamente na fila.");
            Require(client.DataRowCount == 1, "O reprocessamento duplicou uma presença já existente.");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }

    private static async Task AmbiguousAppendAsync()
    {
        var client = new InMemoryGoogleSheetsClient
        {
            FailAfterSuccessfulDataAppendOnce = true
        };

        var repository = CreateRepository(client);
        var attendance = CreateAttendance("999999", "ambiguous-device");

        var first = await repository.TryRegisterAsync(attendance);
        var retry = await repository.TryRegisterAsync(attendance);

        Require(first == AttendancePersistenceStatus.Unavailable, "A falha ambígua não foi reportada como indisponibilidade.");
        Require(retry == AttendancePersistenceStatus.Duplicate, "O retry não detectou a linha já gravada remotamente.");
        Require(client.DataRowCount == 1, "A falha ambígua produziu registro duplicado.");
    }

    private static int ParseOperations(string[] args)
        => args.Length > 0 && int.TryParse(args[0], out var value) && value > 0
            ? value
            : 500;

    private static GoogleSheetsAttendanceRepository CreateRepository(InMemoryGoogleSheetsClient client)
        => new(client, NullLogger<GoogleSheetsAttendanceRepository>.Instance);

    private static DomainActivity CreateActivity()
        => new(
            "atividade-1",
            "Atividade Teste",
            DateOnly.FromDateTime(RegisteredAt.DateTime),
            new AttendanceWindow(new TimeOnly(8, 0), new TimeOnly(9, 0)),
            new AttendanceWindow(new TimeOnly(17, 0), new TimeOnly(18, 0)));

    private static Attendance CreateAttendance(string ra, string deviceId)
        => new(
            "atividade-1",
            "Atividade Teste",
            $"Aluno {ra}",
            ra,
            deviceId,
            AttendanceType.Entry,
            RegisteredAt);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

internal sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
{
    public DateTimeOffset GetNow() => now;
}

internal sealed class FixedActivityRepository(DomainActivity activity) : IActivityRepository
{
    private readonly DomainActivity _activity = activity;

    public Task<DomainActivity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DomainActivity?>(_activity);
    }
}

internal sealed class InMemoryGoogleSheetsClient : IGoogleSheetsClient
{
    private readonly ConcurrentDictionary<string, List<IList<object>>> _sheets =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public bool Available { get; set; } = true;
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
        EnsureAvailable();
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
        EnsureAvailable();

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
    {
        EnsureAvailable();
        return Task.FromResult(_sheets.ContainsKey(sheetName));
    }

    public Task CreateSheetAsync(
        string sheetName,
        CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        _sheets.TryAdd(sheetName, new List<IList<object>>());
        return Task.CompletedTask;
    }

    private void EnsureAvailable()
    {
        if (!Available)
            throw new HttpRequestException("Google Sheets indisponível para a simulação.");
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

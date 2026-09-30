namespace ControlePresenca.Application.Activities.Models;

public sealed record ActivityModel(
    string Id,
    string Name,
    DateOnly Date,
    TimeOnly EntryStartAt,
    TimeOnly EntryEndAt,
    TimeOnly ExitStartAt,
    TimeOnly ExitEndAt);

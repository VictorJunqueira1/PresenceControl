namespace ControlePresenca.Application.Attendances.Commands.ReprocessPendingAttendances;

public sealed record ReprocessPendingAttendancesResult(
    int Total,
    int Registered,
    int AlreadyRegistered,
    int Conflicts,
    int Remaining);

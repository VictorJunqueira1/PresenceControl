using ControlePresenca.Application.Core.Mediator.Commands;

namespace ControlePresenca.Application.Attendances.Commands.ReprocessPendingAttendances;

public sealed record ReprocessPendingAttendancesCommand : ICommand<ReprocessPendingAttendancesResult>
{
}

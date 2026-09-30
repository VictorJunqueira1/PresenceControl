using ControlePresenca.Application.Core.Mediator.Commands;

namespace ControlePresenca.Application.Attendances.Commands.RegisterAttendance;

public sealed record RegisterAttendanceCommand(
    string ActivityId,
    string StudentName,
    string RA,
    string DeviceId) : ICommand<RegisterAttendanceResponse>;

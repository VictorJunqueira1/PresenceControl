namespace ControlePresenca.Application.Attendances.Persistence;

public enum AttendancePersistenceStatus
{
    Registered = 1,
    Duplicate = 2,
    Unavailable = 3,
    DeviceAlreadyUsed = 4
}
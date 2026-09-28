namespace ControlePresenca.Application.Enums;

public enum RegisterAttendanceStatus
{
    Success = 1,
    ActivityNotFound = 2,
    AttendanceWindowClosed = 3,
    AlreadyRegistered = 4,
    RetryRequired = 5,
    StoredForRetry = 6,
    InvalidRequest = 7,
    Failed = 8,
    DeviceAlreadyUsed = 9
}
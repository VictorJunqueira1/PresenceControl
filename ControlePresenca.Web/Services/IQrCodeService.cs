namespace ControlePresenca.Web.Services;

public interface IQrCodeService
{
    string GenerateAttendanceQrCode(string activityId);
    string BuildAttendanceUrl(string activityId);
}
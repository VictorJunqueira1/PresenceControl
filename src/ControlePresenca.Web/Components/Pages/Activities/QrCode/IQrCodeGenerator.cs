namespace ControlePresenca.Web.Components.Pages.Activities.QrCode;

public interface IQrCodeGenerator
{
    string GenerateAttendanceQrCode(string activityId);
    string BuildAttendanceUrl(string activityId);
}
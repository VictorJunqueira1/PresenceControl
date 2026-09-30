using ControlePresenca.Web.Configurations;
using Microsoft.Extensions.Options;
using QRCoder;

namespace ControlePresenca.Web.Components.Pages.Activities.QrCode;

public sealed class QrCodeGenerator : IQrCodeGenerator
{
    private readonly ApplicationOptions _options;

    public QrCodeGenerator(IOptions<ApplicationOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            throw new InvalidOperationException("Application:PublicBaseUrl não foi configurado.");
    }

    public string GenerateAttendanceQrCode(string activityId)
    {
        var url = BuildAttendanceUrl(activityId);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

        using var qrCode = new PngByteQRCode(data);

        var bytes = qrCode.GetGraphic(20);
        var base64 = Convert.ToBase64String(bytes);

        return $"data:image/png;base64,{base64}";
    }

    public string BuildAttendanceUrl(string activityId)
    {
        if (string.IsNullOrWhiteSpace(activityId))
            throw new ArgumentException("O identificador da atividade é obrigatório.", nameof(activityId));

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        var normalizedActivityId = activityId.Trim();

        return $"{baseUrl}/presenca/{Uri.EscapeDataString(normalizedActivityId)}";
    }
}
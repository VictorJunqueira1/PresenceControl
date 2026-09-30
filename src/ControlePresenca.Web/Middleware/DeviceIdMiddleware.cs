namespace ControlePresenca.Web.Middleware;

public sealed class DeviceIdMiddleware
{
    public const string CookieName = "controle-presenca-device";

    private readonly RequestDelegate _next;

    public DeviceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var hasValidDeviceId = context.Request.Cookies.TryGetValue(CookieName, out var deviceId) && Guid.TryParse(deviceId, out _);

        if (!hasValidDeviceId)
        {
            deviceId = Guid.NewGuid().ToString();

            context.Response.Cookies.Append(
                CookieName,
                deviceId,
                new CookieOptions
                {
                    HttpOnly = false,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = context.Request.IsHttps,
                    MaxAge = TimeSpan.FromDays(365),
                    Path = "/"
                });
        }

        await _next(context);
    }
}
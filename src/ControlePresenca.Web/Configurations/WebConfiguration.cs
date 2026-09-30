using ControlePresenca.Web.Components;
using ControlePresenca.Web.Components.Pages.Activities.QrCode;
using ControlePresenca.Web.Middleware;

namespace ControlePresenca.Web.Configurations;

public static class WebConfiguration
{
    public static IServiceCollection AddWebConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.Configure<ApplicationOptions>(configuration.GetSection(ApplicationOptions.SectionName));
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

        return services;
    }

    public static WebApplication UseWebConfiguration(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseMiddleware<DeviceIdMiddleware>();
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        return app;
    }
}
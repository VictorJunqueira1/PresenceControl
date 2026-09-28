using ControlePresenca.Application.Contracts;
using ControlePresenca.Application.UseCases.Attendances.Register;
using ControlePresenca.Infrastructure.DateTime;
using ControlePresenca.Infrastructure.GoogleSheets;
using ControlePresenca.Infrastructure.GoogleSheets.Repositories;
using ControlePresenca.Infrastructure.Pending;
using ControlePresenca.Web.Components;
using ControlePresenca.Web.Configuration;
using ControlePresenca.Web.Middleware;
using ControlePresenca.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<GoogleSheetsOptions>(builder.Configuration.GetSection(GoogleSheetsOptions.SectionName));
builder.Services.AddSingleton<GoogleSheetsClient>();
builder.Services.AddSingleton<QrCodeService>();
builder.Services.AddSingleton<IDateTimeProvider, SaoPauloDateTimeProvider>();
builder.Services.AddSingleton<IActivityRepository, GoogleSheetsActivityRepository>();
builder.Services.AddSingleton<IAttendanceRepository, GoogleSheetsAttendanceRepository>();
builder.Services.AddSingleton<IPendingAttendanceStore>(_ =>
{
    var filePath = Path.Combine(
        builder.Environment.ContentRootPath,
        "App_Data",
        "pending-attendances.json");

    return new JsonPendingAttendanceStore(filePath);
});

builder.Services.AddScoped<RegisterAttendanceUseCase>();
builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection(ApplicationOptions.SectionName));
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<DeviceIdMiddleware>();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
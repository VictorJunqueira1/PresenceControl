using ControlePresenca.Application.Activities.Models;
using ControlePresenca.Application.Activities.Queries.GetActivityById;
using ControlePresenca.Application.Core.Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ControlePresenca.Web.Components.Pages.Activities.QrCode;

public partial class QrCode : IAsyncDisposable
{
    [Parameter]
    public string ActivityId { get; set; } = string.Empty;

    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private IQrCodeGenerator QrCodeGenerator { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private ActivityModel? _activity;
    private IJSObjectReference? _displayModule;

    private ElementReference _qrDisplay;
    private string _qrCode = string.Empty;
    private string _attendanceUrl = string.Empty;
    private bool _loading = true;
    private string? _loadError;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _loadError = null;

        try
        {
            _activity = await Mediator.Send(new GetActivityByIdQuery(ActivityId));

            if (_activity is null)
            {
                _loadError = "Atividade não encontrada.";
                return;
            }

            _attendanceUrl = QrCodeGenerator.BuildAttendanceUrl(_activity.Id);
            _qrCode = QrCodeGenerator.GenerateAttendanceQrCode(_activity.Id);
        }
        catch
        {
            _loadError = "Não foi possível carregar a atividade.";
        }
        finally
        {
            _loading = false;
        }
    }

    private Task PrintAsync()
        => JS.InvokeVoidAsync("window.print").AsTask();

    private async Task ToggleFullscreenAsync()
    {
        _displayModule ??= await JS.InvokeAsync<IJSObjectReference>(
            "import",
            "./js/display.js");

        await _displayModule.InvokeVoidAsync(
            "toggleFullscreen",
            _qrDisplay);
    }

    public async ValueTask DisposeAsync()
    {
        if (_displayModule is not null)
            await _displayModule.DisposeAsync();
    }
}
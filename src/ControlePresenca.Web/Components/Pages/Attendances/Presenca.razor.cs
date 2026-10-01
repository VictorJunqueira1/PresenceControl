using ControlePresenca.Application.Activities.Models;
using ControlePresenca.Application.Activities.Queries.GetActivityById;
using ControlePresenca.Application.Attendances.Commands.RegisterAttendance;
using ControlePresenca.Application.Core.Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ControlePresenca.Web.Components.Pages.Attendances;

public partial class Presenca : IAsyncDisposable
{
    [Parameter]
    public string ActivityId { get; set; } = string.Empty;

    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private readonly AttendanceFormModel _form = new();

    private ActivityModel? _activity;
    private IJSObjectReference? _deviceModule;

    private bool _loading = true;
    private bool _submitting;
    private bool _retryRequired;
    private bool _registrationFinished;

    private string? _loadError;
    private string? _message;
    private string _messageCssClass = "form-message form-message--error";
    private string _deviceId = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _loadError = null;

        try
        {
            _activity = await Mediator.Send(new GetActivityByIdQuery(ActivityId));

            if (_activity is null)
                _loadError = "Atividade não encontrada.";
        }
        catch
        {
            _loadError = "Não foi possível carregar a atividade. Tente novamente.";
        }
        finally
        {
            _loading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _deviceModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/device.js");
            _deviceId = await _deviceModule.InvokeAsync<string>("getDeviceId");

            StateHasChanged();
        }
        catch
        {
            _message = "Não foi possível identificar este dispositivo.";
            _messageCssClass = "form-message form-message--error";

            StateHasChanged();
        }
    }

    private async Task RegisterAsync()
    {
        if (_activity is null || _submitting || string.IsNullOrWhiteSpace(_deviceId))
            return;

        _submitting = true;
        _retryRequired = false;
        _message = null;

        try
        {
            var command = new RegisterAttendanceCommand(
                _activity.Id,
                _form.Name,
                _form.RA,
                _deviceId);

            var response = await Mediator.Send(command);

            HandleResponse(response);
        }
        catch
        {
            SetMessage(
                "Ocorreu um erro inesperado. Tente novamente.",
                "form-message form-message--error");
        }
        finally
        {
            _submitting = false;
        }
    }

    private void HandleResponse(RegisterAttendanceResponse response)
    {
        switch (response.Status)
        {
            case RegisterAttendanceStatus.Success:
                _registrationFinished = true;
                SetMessage(
                    "Seu registro foi realizado com sucesso.",
                    "form-message form-message--success");
                break;

            case RegisterAttendanceStatus.StoredForRetry:
                _registrationFinished = true;
                SetMessage(
                    "Seu registro foi recebido e será processado assim que a conexão for restabelecida.",
                    "form-message form-message--success");
                break;

            case RegisterAttendanceStatus.AlreadyRegistered:
                SetMessage(
                    "Presença já registrada para esta atividade.",
                    "form-message form-message--warning");
                break;

            case RegisterAttendanceStatus.AttendanceWindowClosed:
                SetMessage(
                    "O período para registro desta presença está encerrado.",
                    "form-message form-message--warning");
                break;

            case RegisterAttendanceStatus.ActivityNotFound:
                SetMessage(
                    "Atividade não encontrada.",
                    "form-message form-message--error");
                break;

            case RegisterAttendanceStatus.RetryRequired:
                _retryRequired = true;
                SetMessage(
                    "Não foi possível registrar sua presença. Clique em tentar novamente.",
                    "form-message form-message--warning");
                break;

            case RegisterAttendanceStatus.InvalidRequest:
                SetMessage(
                    "Verifique os dados informados.",
                    "form-message form-message--warning");
                break;

            case RegisterAttendanceStatus.DeviceAlreadyUsed:
                SetMessage(
                    "Este dispositivo já realizou um registro nesta atividade.",
                    "form-message form-message--warning");
                break;

            default:
                SetMessage(
                    "Não foi possível registrar sua presença.",
                    "form-message form-message--error");
                break;
        }
    }

    private void SetMessage(string message, string cssClass)
    {
        _message = message;
        _messageCssClass = cssClass;
    }

    public async ValueTask DisposeAsync()
    {
        if (_deviceModule is not null)
            await _deviceModule.DisposeAsync();
    }
}
using Microsoft.AspNetCore.Components;

namespace ControlePresenca.Web.Components.Pages;

public partial class Error
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private void Reload() => NavigationManager.Refresh(forceReload: true);
}
using System.ComponentModel.DataAnnotations;

namespace ControlePresenca.Web.Models;

public sealed class AttendanceFormModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu RA.")]
    public string RA { get; set; } = string.Empty;
}
namespace ControlePresenca.Web.Configurations;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string PublicBaseUrl { get; set; } = string.Empty;
}
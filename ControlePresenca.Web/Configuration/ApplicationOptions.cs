namespace ControlePresenca.Web.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string PublicBaseUrl { get; set; } = string.Empty;
}
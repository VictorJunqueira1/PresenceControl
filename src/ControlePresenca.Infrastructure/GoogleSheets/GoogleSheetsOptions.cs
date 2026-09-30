namespace ControlePresenca.Infrastructure.GoogleSheets;

public sealed class GoogleSheetsOptions
{
    public const string SectionName = "GoogleSheets";

    public string SpreadsheetId { get; set; } = string.Empty;
    public string CredentialsFilePath { get; set; } = string.Empty;
    public string ActivitiesSheetName { get; set; } = "Activities";
}
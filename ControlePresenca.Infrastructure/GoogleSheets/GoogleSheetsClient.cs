using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Options;

namespace ControlePresenca.Infrastructure.GoogleSheets;

public sealed class GoogleSheetsClient
{
    private readonly SheetsService _service;
    private readonly GoogleSheetsOptions _options;

    public GoogleSheetsClient(IOptions<GoogleSheetsOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SpreadsheetId))
            throw new InvalidOperationException("GoogleSheets:SpreadsheetId não foi configurado.");

        if (string.IsNullOrWhiteSpace(_options.CredentialsFilePath))
            throw new InvalidOperationException("GoogleSheets:CredentialsFilePath não foi configurado.");

        var credential = GoogleCredential
            .FromFile(_options.CredentialsFilePath)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        _service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ControlePresenca"
        });
    }

    public async Task<IList<IList<object>>> ReadAsync(string range, CancellationToken cancellationToken = default)
    {
        var request = _service.Spreadsheets.Values.Get(_options.SpreadsheetId, range);
        var response = await request.ExecuteAsync(cancellationToken);

        return response.Values ?? new List<IList<object>>();
    }

    public async Task AppendRowAsync(
        string sheetName,
        IList<object> values,
        CancellationToken cancellationToken = default)
    {
        var range = $"{QuoteSheetName(sheetName)}!A:H";

        var body = new ValueRange
        {
            Values = new List<IList<object>>
            {
                values
            }
        };

        var request = _service.Spreadsheets.Values.Append(body, _options.SpreadsheetId, range);

        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
        request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

        await request.ExecuteAsync(cancellationToken);
    }

    public async Task<bool> SheetExistsAsync(string sheetName, CancellationToken cancellationToken = default)
    {
        var request = _service.Spreadsheets.Get(_options.SpreadsheetId);
        request.Fields = "sheets.properties.title";

        var spreadsheet = await request.ExecuteAsync(cancellationToken);

        return spreadsheet.Sheets?.Any(sheet => string.Equals(
            sheet.Properties?.Title, 
            sheetName,
            StringComparison.OrdinalIgnoreCase)) == true;
    }

    public async Task CreateSheetAsync(string sheetName, CancellationToken cancellationToken = default)
    {
        var requestBody = new BatchUpdateSpreadsheetRequest
        {
            Requests = new List<Request>
            {
                new()
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = new SheetProperties
                        {
                            Title = sheetName
                        }
                    }
                }
            }
        };

        var request = _service.Spreadsheets.BatchUpdate(requestBody, _options.SpreadsheetId);

        await request.ExecuteAsync(cancellationToken);
    }

    public static string QuoteSheetName(string sheetName)
    {
        var escaped = sheetName.Replace("'", "''");
        return $"'{escaped}'";
    }
}
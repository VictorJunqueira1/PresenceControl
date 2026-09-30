namespace ControlePresenca.Infrastructure.GoogleSheets;

public interface IGoogleSheetsClient
{
    Task<IList<IList<object>>> ReadAsync(string range, CancellationToken cancellationToken = default);
    Task AppendRowAsync(string sheetName, IList<object> values, CancellationToken cancellationToken = default);
    Task<bool> SheetExistsAsync(string sheetName, CancellationToken cancellationToken = default);
    Task CreateSheetAsync(string sheetName, CancellationToken cancellationToken = default);
}

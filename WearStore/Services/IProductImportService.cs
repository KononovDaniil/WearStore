using WearStore.Models;

namespace WearStore.Services;

public interface IProductImportService
{
    ImportResult ImportFromCsv(string filePath);
    ImportResult ImportFromLines(IEnumerable<string> lines);
}
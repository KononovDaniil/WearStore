using System.Globalization;
using WearStore.Models;

namespace WearStore.Services;

public class ProductImportService : IProductImportService
{
    private const string FileNotFoundMessage = "Файл не найден";
    private const string FileReadErrorMessage = "Не удалось прочитать файл";
    private const string NoDataMessage = "Данные для импорта отсутствуют";
    private const string InvalidHeaderMessage = "Неверные заголовки CSV";
    private const string WrongFieldCountMessage = "Неверное количество полей в строке";
    private const string DuplicateSkuMessage = "Артикул товара уже существует";
    private const string EmptySkuMessage = "Поле sku не может быть пустым";
    private const string EmptyNameMessage = "Поле name не может быть пустым";
    private const string InvalidPriceMessage = "Поле price должно быть числом больше 0";
    private const string InvalidStockMessage = "Остаток на складе должен быть целым числом не меньше 0";
    private const string InvalidActiveMessage = "Поле is_active должно иметь значение true, false, 1 или 0";
    private const string EmptyColorMessage = "Поле color обязательно для варианта «Одежда»";

    private static readonly string InvalidSizeMessage =
        $"Размер должен быть XS, S, M, L, XL, XXL или числом от {ImportConstants.MinNumericSize} до {ImportConstants.MaxNumericSize}";

    private static readonly string[] RequiredColumns =
    {
        "sku", "name", "description", "price", "currency", "category_name",
        "stock", "image_url", "is_active", "size", "color", "material"
    };

    private readonly IProductRepository _repository;

    public ProductImportService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public ImportResult ImportFromCsv(string filePath)
    {
        var result = new ImportResult();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            AddError(result, 0, FileNotFoundMessage);
            return result;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            AddError(result, 0, FileReadErrorMessage);
            return result;
        }

        return ImportFromLines(lines);
    }

    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        var result = new ImportResult();
        var allLines = lines?.ToList() ?? new List<string>();

        int headerIndex = allLines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
        if (headerIndex < 0)
        {
            AddError(result, 0, NoDataMessage);
            return result;
        }

        var headerFields = CsvLineParser.Parse(allLines[headerIndex]);
        var columnMap = BuildColumnMap(headerFields);

        if (RequiredColumns.Any(column => !columnMap.ContainsKey(column)))
        {
            AddError(result, headerIndex + 1, InvalidHeaderMessage);
            return result;
        }

        for (int index = headerIndex + 1; index < allLines.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(allLines[index]))
                continue;

            result.TotalRows++;
            int rowNumber = index + 1;
            var values = CsvLineParser.Parse(allLines[index]);

            if (values.Count != headerFields.Count)
            {
                AddError(result, rowNumber, WrongFieldCountMessage);
                continue;
            }

            var product = ParseProduct(values, columnMap, rowNumber, result.Errors);
            if (product == null)
                continue;

            if (_repository.GetBySku(product.Sku) != null)
            {
                AddError(result, rowNumber, DuplicateSkuMessage);
                continue;
            }

            _repository.Add(product);
            result.ImportedCount++;
        }

        if (result.TotalRows == 0 && result.Errors.Count == 0)
            AddError(result, 0, NoDataMessage);

        return result;
    }

    private static Product? ParseProduct(
        IReadOnlyList<string> values,
        Dictionary<string, int> columnMap,
        int rowNumber,
        List<ImportError> errors)
    {
        int errorsBefore = errors.Count;

        string Get(string column) => values[columnMap[column]].Trim();

        void Fail(string message) =>
            errors.Add(new ImportError { RowNumber = rowNumber, Message = message });

        string sku = Get("sku");
        if (sku.Length == 0)
            Fail(EmptySkuMessage);

        string name = Get("name");
        if (name.Length == 0)
            Fail(EmptyNameMessage);

        if (!TryParseDecimal(Get("price"), out decimal price) || price <= 0)
            Fail(InvalidPriceMessage);

        if (!TryParseInt(Get("stock"), out int stock) || stock < 0)
            Fail(InvalidStockMessage);

        if (!TryParseBool(Get("is_active"), out bool isActive))
            Fail(InvalidActiveMessage);

        if (!TryNormalizeSize(Get("size"), out string size))
            Fail(InvalidSizeMessage);

        string color = Get("color");
        if (color.Length == 0)
            Fail(EmptyColorMessage);

        if (errors.Count > errorsBefore)
            return null;

        return new Product
        {
            Sku = sku,
            Name = name,
            Description = Get("description"),
            Price = price,
            Currency = NormalizeCurrency(Get("currency")),
            CategoryName = NormalizeCategory(Get("category_name")),
            StockQuantity = stock,
            ImageUrl = Get("image_url"),
            IsActive = isActive,
            Size = size,
            Color = color,
            Material = Get("material")
        };
    }

    private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string> headerFields)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerFields.Count; i++)
        {
            string name = headerFields[i].Trim().TrimStart('\uFEFF').Trim();
            if (name.Length > 0 && !map.ContainsKey(name))
                map[name] = i;
        }

        return map;
    }

    private static void AddError(ImportResult result, int rowNumber, string message)
    {
        result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = message });
    }

    private static string NormalizeCurrency(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ImportConstants.DefaultCurrency
            : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeCategory(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ImportConstants.DefaultCategory
            : value.Trim();
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(
            value.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(
            value,
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryParseBool(string value, out bool result)
    {
        result = false;

        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1")
        {
            result = true;
            return true;
        }

        return value.Equals("false", StringComparison.OrdinalIgnoreCase) || value == "0";
    }

    private static bool TryNormalizeSize(string value, out string size)
    {
        size = string.Empty;
        string trimmed = value.Trim();

        string? letterSize = ImportConstants.LetterSizes.FirstOrDefault(s =>
            string.Equals(s, trimmed, StringComparison.OrdinalIgnoreCase));

        if (letterSize != null)
        {
            size = letterSize;
            return true;
        }

        if (TryParseInt(trimmed, out int number)
            && number >= ImportConstants.MinNumericSize
            && number <= ImportConstants.MaxNumericSize)
        {
            size = number.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }
}
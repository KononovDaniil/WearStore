using WearStore.Models;
using WearStore.Services;

namespace WearStore.Tests;

public class ProductImportServiceTests
{
    private const string Header =
        "sku,name,description,price,currency,category_name,stock,image_url,is_active,size,color,material";

    private static string Row(
        string sku = "3001",
        string name = "Футболка белая",
        string description = "Летняя футболка",
        string price = "990",
        string currency = "RUB",
        string category = "Одежда",
        string stock = "25",
        string image = "/images/tshirt.jpg",
        string active = "true",
        string size = "M",
        string color = "белый",
        string material = "хлопок")
    {
        return string.Join(",", sku, name, description, price, currency, category,
            stock, image, active, size, color, material);
    }

    private static (ImportResult Result, InMemoryProductRepository Repository) Import(params string[] dataRows)
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductImportService(repository);
        var lines = new[] { Header }.Concat(dataRows);
        return (service.ImportFromLines(lines), repository);
    }

    private static void AssertRejected(string row, string expectedMessage)
    {
        var (result, repository) = Import(row);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, repository.Count);
        Assert.Contains(result.Errors, e => e.Message.Contains(expectedMessage));
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ValidRow_IsImported()
    {
        var (result, repository) = Import(Row());

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var product = repository.GetBySku("3001");
        Assert.NotNull(product);
        Assert.Equal("Футболка белая", product.Name);
        Assert.Equal(990m, product.Price);
        Assert.Equal("M", product.Size);
        Assert.Equal("белый", product.Color);
        Assert.Equal("хлопок", product.Material);
    }

    [Fact]
    public void SeveralValidRows_AreImported()
    {
        var (result, repository) = Import(Row("3001"), Row("3002", name: "Джинсы синие", size: "46"));

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, repository.Count);
    }

    [Fact]
    public void FileNotFound_ReturnsError()
    {
        var service = new ProductImportService(new InMemoryProductRepository());
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");

        var result = service.ImportFromCsv(path);

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Файл не найден", result.Errors[0].Message);
    }

    [Fact]
    public void EmptyFile_ReturnsNoDataError()
    {
        var service = new ProductImportService(new InMemoryProductRepository());
        var path = CreateTempFile(string.Empty);

        try
        {
            var result = service.ImportFromCsv(path);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OnlyHeader_ReturnsNoDataError()
    {
        var (result, _) = Import();

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
    }

    [Fact]
    public void WrongHeader_ReturnsError()
    {
        var service = new ProductImportService(new InMemoryProductRepository());
        var lines = new[] { "sku,name,description,price,currency,category_name,stock,image_url,is_active" };

        var result = service.ImportFromLines(lines);

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Неверные заголовки CSV", result.Errors[0].Message);
    }

    [Fact]
    public void WrongFieldCount_ReturnsError()
    {
        AssertRejected("3001,Футболка белая", "Неверное количество полей в строке");
    }

    [Fact]
    public void EmptySku_ReturnsError()
    {
        AssertRejected(Row(sku: ""), "Поле sku не может быть пустым");
    }

    [Fact]
    public void EmptyName_ReturnsError()
    {
        AssertRejected(Row(name: ""), "Поле name не может быть пустым");
    }

    [Fact]
    public void NegativePrice_ReturnsErrorWithRowNumber()
    {
        var (result, repository) = Import(Row(price: "-500"));

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, repository.Count);
        Assert.Single(result.Errors);
        Assert.Equal(2, result.Errors[0].RowNumber);
        Assert.Contains("Поле price должно быть числом больше 0", result.Errors[0].Message);
    }

    [Fact]
    public void NonNumericPrice_ReturnsError()
    {
        AssertRejected(Row(price: "abc"), "Поле price должно быть числом больше 0");
    }

    [Fact]
    public void NegativeStock_ReturnsError()
    {
        AssertRejected(Row(stock: "-5"), "Остаток на складе должен быть целым числом не меньше 0");
    }

    [Fact]
    public void BadIsActive_ReturnsError()
    {
        AssertRejected(Row(active: "yes"), "Поле is_active должно иметь значение true, false, 1 или 0");
    }

    [Fact]
    public void InvalidLetterSize_ReturnsError()
    {
        AssertRejected(Row(size: "XXXL"), "Размер должен быть");
    }

    [Fact]
    public void NumericSizeOutOfRange_ReturnsError()
    {
        AssertRejected(Row(size: "70"), "Размер должен быть");
    }

    [Fact]
    public void EmptyColor_ReturnsError()
    {
        AssertRejected(Row(color: ""), "Поле color обязательно для варианта «Одежда»");
    }

    [Fact]
    public void ValidSizes_AreAcceptedAndNormalized()
    {
        var (result, repository) = Import(Row("3001", size: "m"), Row("3002", size: "46"));

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal("M", repository.GetBySku("3001")!.Size);
        Assert.Equal("46", repository.GetBySku("3002")!.Size);
    }

    [Fact]
    public void DuplicateSkuInsideFile_SecondRowSkipped()
    {
        var (result, repository) = Import(
            Row("3001", name: "Футболка белая"),
            Row("3001", name: "Футболка чёрная", color: "чёрный"));

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
        Assert.Equal("Футболка белая", repository.GetBySku("3001")!.Name);
    }

    [Fact]
    public void DuplicateSkuInRepository_RowSkipped()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(new Product { Sku = "3001", Name = "Старый товар" });
        var service = new ProductImportService(repository);

        var result = service.ImportFromLines(new[] { Header, Row("3001") });

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, repository.Count);
        Assert.Single(result.Errors);
        Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
    }

    [Fact]
    public void SeveralErrorsInOneRow_AllReported()
    {
        var (result, repository) = Import(Row(
            sku: "", name: "", price: "-10", stock: "-5", active: "yes", size: "XXXL", color: ""));

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(7, result.ErrorCount);
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    public void EmptyOptionalFields_AreAllowed()
    {
        var (result, repository) = Import(Row(description: "", image: "", material: ""));

        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var product = repository.GetBySku("3001")!;
        Assert.Equal(string.Empty, product.Description);
        Assert.Equal(string.Empty, product.ImageUrl);
        Assert.Equal(string.Empty, product.Material);
    }

    [Fact]
    public void EmptyCurrency_BecomesRub()
    {
        var (_, repository) = Import(Row(currency: ""));

        Assert.Equal("RUB", repository.GetBySku("3001")!.Currency);
    }

    [Fact]
    public void EmptyCategory_BecomesDefault()
    {
        var (_, repository) = Import(Row(category: ""));

        Assert.Equal("Без категории", repository.GetBySku("3001")!.CategoryName);
    }

    [Fact]
    public void ValidFile_WithCyrillic_IsImported()
    {
        var content = string.Join(Environment.NewLine, Header, Row());
        var path = CreateTempFile(content);
        var repository = new InMemoryProductRepository();
        var service = new ProductImportService(repository);

        try
        {
            var result = service.ImportFromCsv(path);

            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);
            Assert.Equal("Футболка белая", repository.GetBySku("3001")!.Name);
            Assert.Equal("Одежда", repository.GetBySku("3001")!.CategoryName);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
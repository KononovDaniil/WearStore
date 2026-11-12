using Microsoft.AspNetCore.Mvc;
using WearStore.Models;
using WearStore.Services;

namespace WearStore.Controllers;

public class ProductsController : Controller
{
    private const string EmptyPathMessage = "Укажите путь к CSV-файлу";
    private const string NoFileSelectedMessage = "Выберите CSV-файл";

    private readonly IProductRepository _repository;
    private readonly IProductImportService _importService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        IProductRepository repository,
        IProductImportService importService,
        IWebHostEnvironment environment)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    [HttpGet]
    public IActionResult Index(string? search, string? category, string? sort)
    {
        var all = _repository.GetAll();
        IEnumerable<Product> query = all;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => string.Equals(p.CategoryName, category, StringComparison.OrdinalIgnoreCase));
        }

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "stock_asc" => query.OrderBy(p => p.StockQuantity),
            "stock_desc" => query.OrderByDescending(p => p.StockQuantity),
            _ => query
        };

        var viewModel = new ProductsIndexViewModel
        {
            Products = query.ToList(),
            Categories = all.Select(p => p.CategoryName).Distinct().OrderBy(c => c).ToList(),
            Search = search?.Trim() ?? string.Empty,
            Category = category ?? string.Empty,
            Sort = sort ?? string.Empty,
            AllCount = all.Count
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Import()
    {
        return View(new ImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Import(string? filePath)
    {
        string? resolvedPath = ResolveFilePath(filePath);
        ImportResult result;

        if (resolvedPath == null)
        {
            result = new ImportResult();
            result.Errors.Add(new ImportError { RowNumber = 0, Message = EmptyPathMessage });
        }
        else
        {
            result = _importService.ImportFromCsv(resolvedPath);
        }

        return View(new ImportViewModel
        {
            FilePath = filePath ?? string.Empty,
            Result = result
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ImportUpload(IFormFile? csvFile)
    {
        ImportResult result;

        if (csvFile == null)
        {
            result = new ImportResult();
            result.Errors.Add(new ImportError { RowNumber = 0, Message = NoFileSelectedMessage });
        }
        else
        {
            using var stream = csvFile.OpenReadStream();
            result = _importService.ImportFromStream(stream);
        }

        return View("Import", new ImportViewModel { Result = result });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _repository.Clear();
        return RedirectToAction(nameof(Index));
    }

    private string? ResolveFilePath(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        string trimmed = input.Trim().Trim('"');

        if (Path.IsPathFullyQualified(trimmed))
            return trimmed;

        return Path.Combine(_environment.WebRootPath, trimmed.TrimStart('/', '\\'));
    }
}
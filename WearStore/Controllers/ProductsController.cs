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
    public IActionResult Index()
    {
        var viewModel = new ProductsIndexViewModel
        {
            Products = _repository.GetAll()
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
namespace WearStore.Models;

public class ProductsIndexViewModel
{
    public IReadOnlyList<Product> Products { get; set; } = new List<Product>();
    public IReadOnlyList<string> Categories { get; set; } = new List<string>();
    public string Search { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Sort { get; set; } = string.Empty;
    public int AllCount { get; set; }
    public int TotalCount => Products.Count;
    public decimal TotalValue => Products.Sum(p => p.Price);
}
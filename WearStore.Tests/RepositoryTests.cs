using WearStore.Models;
using WearStore.Services;

namespace WearStore.Tests;

public class RepositoryTests
{
    private static Product CreateProduct(string sku = "1001")
    {
        return new Product
        {
            Sku = sku,
            Name = "Футболка белая",
            Price = 990m,
            Size = "M",
            Color = "белый"
        };
    }

    [Fact]
    public void Add_AddsProduct_GetAllContainsIt()
    {
        var repository = new InMemoryProductRepository();

        repository.Add(CreateProduct());

        var all = repository.GetAll();
        Assert.Single(all);
        Assert.Equal("1001", all[0].Sku);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void GetBySku_ExistingSku_ReturnsProduct()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct());

        var product = repository.GetBySku("1001");

        Assert.NotNull(product);
        Assert.Equal("Футболка белая", product.Name);
    }

    [Fact]
    public void GetBySku_IgnoresSpacesAndCase()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct("ABC1"));

        var product = repository.GetBySku("  abc1 ");

        Assert.NotNull(product);
    }

    [Fact]
    public void EmptyRepository_HasNoProducts()
    {
        var repository = new InMemoryProductRepository();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
        Assert.Null(repository.GetBySku("1001"));
    }

    [Fact]
    public void GetAll_ReturnsSnapshot_ChangesDoNotAffectRepository()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct());

        var all = (List<Product>)repository.GetAll();
        all.Clear();

        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void Clear_RemovesAllProducts()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct());

        repository.Clear();

        Assert.Equal(0, repository.Count);
    }
}
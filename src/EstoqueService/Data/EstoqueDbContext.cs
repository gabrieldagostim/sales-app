using EstoqueService.Models;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Data;

public class EstoqueDbContext : DbContext
{
    public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos => Set<Produto>();
}

/// <summary>
/// Insere os produtos iniciais caso o banco esteja vazio.
/// </summary>
public static class DbInitializer
{
    public static void Seed(EstoqueDbContext db)
    {
        if (db.Produtos.Any())
            return;

        db.Produtos.AddRange(
            new Produto
            {
                Nome = "Notebook Gamer",
                Descricao = "Notebook 16 GB RAM, 512 GB SSD, RTX 4060",
                Preco = 5499.90m,
                Quantidade = 10
            },
            new Produto
            {
                Nome = "Mouse sem fio",
                Descricao = "Mouse óptico sem fio 2.4 GHz",
                Preco = 129.90m,
                Quantidade = 50
            },
            new Produto
            {
                Nome = "Monitor 27\"",
                Descricao = "Monitor LED 27 polegadas Full HD",
                Preco = 1299.90m,
                Quantidade = 8
            });

        db.SaveChanges();
    }
}

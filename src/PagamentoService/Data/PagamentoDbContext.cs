using Microsoft.EntityFrameworkCore;
using PagamentoService.Models;

namespace PagamentoService.Data;

public class PagamentoDbContext : DbContext
{
    public PagamentoDbContext(DbContextOptions<PagamentoDbContext> options) : base(options)
    {
    }

    public DbSet<Venda> Vendas => Set<Venda>();
}

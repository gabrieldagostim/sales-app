using ExpedicaoService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpedicaoService.Data;

public class ExpedicaoDbContext : DbContext
{
    public ExpedicaoDbContext(DbContextOptions<ExpedicaoDbContext> options) : base(options)
    {
    }

    public DbSet<Expedicao> Expedicoes => Set<Expedicao>();
}

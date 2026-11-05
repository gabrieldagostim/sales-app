using DocumentoFiscalService.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentoFiscalService.Data;

public class DocumentoFiscalDbContext : DbContext
{
    public DocumentoFiscalDbContext(DbContextOptions<DocumentoFiscalDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentoFiscal> DocumentosFiscais => Set<DocumentoFiscal>();
}

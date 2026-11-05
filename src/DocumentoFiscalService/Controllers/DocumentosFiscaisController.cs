using DocumentoFiscalService.Data;
using DocumentoFiscalService.Dtos;
using DocumentoFiscalService.Integrations;
using DocumentoFiscalService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentoFiscalService.Controllers;

[ApiController]
[Route("api/documentos-fiscais")]
public class DocumentosFiscaisController : ControllerBase
{
    private readonly DocumentoFiscalDbContext _db;
    private readonly EstoqueApiClient _estoqueClient;
    private readonly ILogger<DocumentosFiscaisController> _logger;

    public DocumentosFiscaisController(
        DocumentoFiscalDbContext db,
        EstoqueApiClient estoqueClient,
        ILogger<DocumentosFiscaisController> logger)
    {
        _db = db;
        _estoqueClient = estoqueClient;
        _logger = logger;
    }

    /// <summary>Lista todos os documentos fiscais emitidos.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentoFiscalResponse>>> Listar()
    {
        var documentos = await _db.DocumentosFiscais.AsNoTracking().ToListAsync();
        return Ok(documentos.Select(ToResponse));
    }

    /// <summary>Retorna um documento fiscal pelo id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocumentoFiscalResponse>> Obter(int id)
    {
        var documento = await _db.DocumentosFiscais.FindAsync(id);
        if (documento is null)
            return NotFound(new { mensagem = $"Documento fiscal {id} não encontrado." });

        return Ok(ToResponse(documento));
    }

    /// <summary>
    /// Emite um documento fiscal, chamado pelo EstoqueService após a baixa de estoque.
    /// Para cada item, consulta o EstoqueService (BUSCA SIMPLES 2) para obter
    /// o nome e o preço atual do produto.
    /// </summary>
    [HttpPost("emitir")]
    public async Task<ActionResult<DocumentoFiscalResponse>> Emitir(EmitirDocumentoRequest request)
    {
        if (request.Itens is null || request.Itens.Count == 0)
            return BadRequest(new { mensagem = "O documento fiscal precisa de pelo menos um item." });

        var itens = new List<ItemDocumentoFiscal>();

        foreach (var item in request.Itens)
        {
            var produto = await _estoqueClient.ObterProdutoAsync(item.ProdutoId);
            if (produto is null)
                return UnprocessableEntity(new
                {
                    mensagem = $"Produto {item.ProdutoId} não encontrado no estoque para emissão do documento fiscal."
                });

            itens.Add(new ItemDocumentoFiscal
            {
                ProdutoId = produto.Id,
                ProdutoNome = produto.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.Preco
            });
        }

        var totalDocumentos = await _db.DocumentosFiscais.CountAsync();
        var numero = $"NF-{DateTime.UtcNow:yyyy}-{totalDocumentos + 1:D6}";

        var documento = new DocumentoFiscal
        {
            Numero = numero,
            VendaId = request.VendaId,
            ClienteNome = request.ClienteNome,
            ClienteEmail = request.ClienteEmail,
            Itens = itens,
            ValorTotal = itens.Sum(i => i.Subtotal),
            Status = "Emitido",
            EmitidoEm = DateTime.UtcNow
        };

        _db.DocumentosFiscais.Add(documento);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Documento fiscal {Numero} emitido para a venda {VendaId}.", numero, request.VendaId);

        return CreatedAtAction(nameof(Obter), new { id = documento.Id }, ToResponse(documento));
    }

    private static DocumentoFiscalResponse ToResponse(DocumentoFiscal d) => new(
        d.Id,
        d.Numero,
        d.VendaId,
        d.ClienteNome,
        d.ClienteEmail,
        d.Itens.Select(i => new ItemDocumentoFiscalResponse(i.ProdutoId, i.ProdutoNome, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList(),
        d.ValorTotal,
        d.Status,
        d.EmitidoEm);
}

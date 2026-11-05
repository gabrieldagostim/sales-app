using ExpedicaoService.Data;
using ExpedicaoService.Dtos;
using ExpedicaoService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpedicaoService.Controllers;

[ApiController]
[Route("api/expedicoes")]
public class ExpedicoesController : ControllerBase
{
    private static readonly string[] StatusValidos = ["Preparando", "Enviada", "Entregue", "Cancelada"];

    private readonly ExpedicaoDbContext _db;
    private readonly ILogger<ExpedicoesController> _logger;

    public ExpedicoesController(ExpedicaoDbContext db, ILogger<ExpedicoesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Lista todas as expedições.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpedicaoResponse>>> Listar()
    {
        var expedicoes = await _db.Expedicoes.AsNoTracking().ToListAsync();
        return Ok(expedicoes.Select(ToResponse));
    }

    /// <summary>Retorna uma expedição pelo id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpedicaoResponse>> Obter(int id)
    {
        var expedicao = await _db.Expedicoes.FindAsync(id);
        if (expedicao is null)
            return NotFound(new { mensagem = $"Expedição {id} não encontrada." });

        return Ok(ToResponse(expedicao));
    }

    /// <summary>
    /// Cria uma expedição, chamado pelo EstoqueService após a emissão do documento fiscal.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExpedicaoResponse>> Criar(CriarExpedicaoRequest request)
    {
        if (request.Itens is null || request.Itens.Count == 0)
            return BadRequest(new { mensagem = "A expedição precisa de pelo menos um item." });

        var expedicao = new Expedicao
        {
            DocumentoFiscalId = request.DocumentoFiscalId,
            NumeroDocumentoFiscal = request.NumeroDocumentoFiscal,
            VendaId = request.VendaId,
            ClienteNome = request.ClienteNome,
            EnderecoEntrega = request.EnderecoEntrega,
            Itens = request.Itens
                .Select(i => new ItemExpedicao
                {
                    ProdutoId = i.ProdutoId,
                    ProdutoNome = i.ProdutoNome,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario
                })
                .ToList(),
            Status = "Preparando"
        };

        _db.Expedicoes.Add(expedicao);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Expedição {ExpedicaoId} criada para a venda {VendaId} (documento fiscal {Numero}).",
            expedicao.Id, request.VendaId, request.NumeroDocumentoFiscal);

        return CreatedAtAction(nameof(Obter), new { id = expedicao.Id }, ToResponse(expedicao));
    }

    /// <summary>Atualiza o status de uma expedição (Preparando, Enviada, Entregue, Cancelada).</summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AtualizarStatus(int id, AtualizarStatusRequest request)
    {
        if (!StatusValidos.Contains(request.Status))
            return BadRequest(new { mensagem = $"Status inválido. Valores permitidos: {string.Join(", ", StatusValidos)}." });

        var expedicao = await _db.Expedicoes.FindAsync(id);
        if (expedicao is null)
            return NotFound(new { mensagem = $"Expedição {id} não encontrada." });

        expedicao.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(expedicao));
    }

    private static ExpedicaoResponse ToResponse(Expedicao e) => new(
        e.Id,
        e.DocumentoFiscalId,
        e.NumeroDocumentoFiscal,
        e.VendaId,
        e.ClienteNome,
        e.EnderecoEntrega,
        e.Itens.Select(i => new ItemExpedicaoResponse(i.ProdutoId, i.ProdutoNome, i.Quantidade, i.PrecoUnitario)).ToList(),
        e.Status,
        e.CriadaEm);
}

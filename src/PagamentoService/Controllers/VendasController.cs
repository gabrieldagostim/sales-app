using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PagamentoService.Data;
using PagamentoService.Dtos;
using PagamentoService.Integrations;
using PagamentoService.Models;

namespace PagamentoService.Controllers;

[ApiController]
[Route("api/vendas")]
public class VendasController : ControllerBase
{
    private readonly PagamentoDbContext _db;
    private readonly EstoqueApiClient _estoqueClient;
    private readonly ILogger<VendasController> _logger;

    public VendasController(
        PagamentoDbContext db,
        EstoqueApiClient estoqueClient,
        ILogger<VendasController> logger)
    {
        _db = db;
        _estoqueClient = estoqueClient;
        _logger = logger;
    }

    /// <summary>Lista todas as vendas registradas.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendaResponse>>> Listar()
    {
        var vendas = await _db.Vendas.AsNoTracking().ToListAsync();
        return Ok(vendas.Select(ToResponse));
    }

    /// <summary>Retorna uma venda pelo id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendaResponse>> Obter(int id)
    {
        var venda = await _db.Vendas.FindAsync(id);
        if (venda is null)
            return NotFound(new { mensagem = $"Venda {id} não encontrada." });

        return Ok(ToResponse(venda));
    }

    /// <summary>
    /// Registra uma nova venda. Para cada item, consulta o EstoqueService
    /// (BUSCA SIMPLES 1) para obter preço e validar disponibilidade.
    /// A venda nasce com o status "Pendente".
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VendaResponse>> RegistrarVenda(CriarVendaRequest request)
    {
        if (request.Itens is null || request.Itens.Count == 0)
            return BadRequest(new { mensagem = "A venda precisa de pelo menos um item." });

        var itens = new List<ItemVenda>();

        foreach (var item in request.Itens)
        {
            var produto = await _estoqueClient.ObterProdutoAsync(item.ProdutoId);
            if (produto is null)
                return UnprocessableEntity(new { mensagem = $"Produto {item.ProdutoId} não encontrado no estoque." });
            if (!produto.Ativo)
                return UnprocessableEntity(new { mensagem = $"Produto '{produto.Nome}' está inativo." });
            if (produto.Quantidade < item.Quantidade)
                return UnprocessableEntity(new
                {
                    mensagem = $"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.Quantidade}, solicitado: {item.Quantidade}."
                });

            itens.Add(new ItemVenda
            {
                ProdutoId = produto.Id,
                ProdutoNome = produto.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.Preco
            });
        }

        var venda = new Venda
        {
            ClienteNome = request.ClienteNome,
            ClienteEmail = request.ClienteEmail,
            EnderecoEntrega = request.EnderecoEntrega,
            Itens = itens,
            ValorTotal = itens.Sum(i => i.Subtotal),
            Status = "Pendente"
        };

        _db.Vendas.Add(venda);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Venda {VendaId} registrada no valor de {ValorTotal}.", venda.Id, venda.ValorTotal);

        return CreatedAtAction(nameof(Obter), new { id = venda.Id }, ToResponse(venda));
    }

    /// <summary>
    /// Confirma o pagamento de uma venda pendente.
    /// Dispara a baixa de estoque no EstoqueService (ALTERAÇÃO DE DADOS) e,
    /// se a baixa for bem-sucedida, a venda passa para o status "Pago".
    /// </summary>
    [HttpPost("{id:int}/pagar")]
    public async Task<ActionResult<PagarVendaResponse>> Pagar(int id)
    {
        var venda = await _db.Vendas.FindAsync(id);
        if (venda is null)
            return NotFound(new { mensagem = $"Venda {id} não encontrada." });

        if (venda.Status != "Pendente")
            return BadRequest(new PagarVendaResponse(false,
                $"A venda já está com o status '{venda.Status}'.", venda.Status));

        var baixaRequest = new BaixaEstoqueRequest(
            venda.Id,
            venda.ClienteNome,
            venda.ClienteEmail,
            venda.EnderecoEntrega,
            venda.Itens.Select(i => new ItemBaixaRequest(i.ProdutoId, i.Quantidade)).ToList());

        var resultado = await _estoqueClient.RegistrarBaixaAsync(baixaRequest);

        if (resultado is null || !resultado.Sucesso)
        {
            venda.Status = "Falha";
            await _db.SaveChangesAsync();

            return UnprocessableEntity(new PagarVendaResponse(false,
                resultado?.Mensagem ?? "Não foi possível dar baixa no estoque.", venda.Status));
        }

        venda.Status = "Pago";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Venda {VendaId} paga. Documento fiscal {Numero} e expedição {ExpedicaoId}.",
            venda.Id, resultado.NumeroDocumentoFiscal, resultado.ExpedicaoId);

        return Ok(new PagarVendaResponse(true,
            "Pagamento confirmado. " + resultado.Mensagem,
            venda.Status,
            resultado.DocumentoFiscalId,
            resultado.NumeroDocumentoFiscal,
            resultado.ExpedicaoId));
    }

    private static VendaResponse ToResponse(Venda v) => new(
        v.Id,
        v.ClienteNome,
        v.ClienteEmail,
        v.EnderecoEntrega,
        v.Itens.Select(i => new ItemVendaResponse(i.ProdutoId, i.ProdutoNome, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList(),
        v.ValorTotal,
        v.Status,
        v.CriadaEm);
}

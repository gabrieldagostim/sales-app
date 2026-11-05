using EstoqueService.Data;
using EstoqueService.Dtos;
using EstoqueService.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Controllers;

[ApiController]
[Route("api/estoque")]
public class EstoqueController : ControllerBase
{
    private readonly EstoqueDbContext _db;
    private readonly DocumentoFiscalApiClient _documentoFiscalClient;
    private readonly ExpedicaoApiClient _expedicaoClient;
    private readonly ILogger<EstoqueController> _logger;

    public EstoqueController(
        EstoqueDbContext db,
        DocumentoFiscalApiClient documentoFiscalClient,
        ExpedicaoApiClient expedicaoClient,
        ILogger<EstoqueController> logger)
    {
        _db = db;
        _documentoFiscalClient = documentoFiscalClient;
        _expedicaoClient = expedicaoClient;
        _logger = logger;
    }

    /// <summary>Visão geral do estoque (produtos e quantidades).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoResponse>>> ConsultarEstoque()
    {
        var produtos = await _db.Produtos.AsNoTracking().ToListAsync();
        return Ok(produtos.Select(p => new ProdutoResponse(p.Id, p.Nome, p.Descricao, p.Preco, p.Quantidade, p.Ativo)));
    }

    /// <summary>
    /// Baixa de estoque, chamada pelo PagamentoService quando um pagamento é confirmado.
    /// Fluxo: dá baixa nos produtos, dispara a emissão do documento fiscal e,
    /// em seguida, a criação da expedição (alterações de dados entre microsserviços).
    /// </summary>
    [HttpPost("baixa")]
    public async Task<ActionResult<BaixaEstoqueResponse>> RegistrarBaixa(BaixaEstoqueRequest request)
    {
        if (request.Itens is null || request.Itens.Count == 0)
            return BadRequest(new { mensagem = "A baixa precisa de pelo menos um item." });

        var produtos = await _db.Produtos
            .Where(p => request.Itens.Select(i => i.ProdutoId).Contains(p.Id))
            .ToListAsync();

        foreach (var item in request.Itens)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProdutoId);
            if (produto is null)
                return UnprocessableEntity(new BaixaEstoqueResponse(false, $"Produto {item.ProdutoId} não encontrado."));
            if (!produto.Ativo)
                return UnprocessableEntity(new BaixaEstoqueResponse(false, $"Produto '{produto.Nome}' está inativo."));
            if (produto.Quantidade < item.Quantidade)
                return UnprocessableEntity(new BaixaEstoqueResponse(false,
                    $"Estoque insuficiente para o produto '{produto.Nome}'. Disponível: {produto.Quantidade}, solicitado: {item.Quantidade}."));
        }

        foreach (var item in request.Itens)
        {
            var produto = produtos.First(p => p.Id == item.ProdutoId);
            produto.Quantidade -= item.Quantidade;
        }

        await _db.SaveChangesAsync();

        var emitirRequest = new EmitirDocumentoFiscalRequest(
            request.VendaId,
            request.ClienteNome,
            request.ClienteEmail,
            request.Itens.Select(i => new ItemDocumentoFiscalRequest(i.ProdutoId, i.Quantidade)).ToList());

        var documento = await _documentoFiscalClient.EmitirAsync(emitirRequest);
        if (documento is null)
        {
            _logger.LogWarning("Venda {VendaId}: baixa realizada, porém o documento fiscal não foi emitido.", request.VendaId);
            return Ok(new BaixaEstoqueResponse(true,
                "Baixa de estoque realizada, porém não foi possível emitir o documento fiscal."));
        }

        var criarExpedicaoRequest = new CriarExpedicaoRequest(
            documento.Id,
            documento.Numero,
            request.VendaId,
            request.ClienteNome,
            request.EnderecoEntrega,
            documento.Itens
                .Select(i => new ItemExpedicaoRequest(i.ProdutoId, i.ProdutoNome, i.Quantidade, i.PrecoUnitario))
                .ToList());

        var expedicao = await _expedicaoClient.CriarAsync(criarExpedicaoRequest);
        if (expedicao is null)
        {
            _logger.LogWarning("Venda {VendaId}: documento fiscal emitido, porém a expedição não foi criada.", request.VendaId);
            return Ok(new BaixaEstoqueResponse(true,
                "Baixa realizada e documento fiscal emitido, porém não foi possível criar a expedição.",
                documento.Id, documento.Numero, null));
        }

        _logger.LogInformation(
            "Venda {VendaId}: estoque baixado, documento fiscal {Numero} emitido e expedição {ExpedicaoId} criada.",
            request.VendaId, documento.Numero, expedicao.Id);

        return Ok(new BaixaEstoqueResponse(
            true,
            "Baixa de estoque realizada. Documento fiscal emitido e expedição criada com sucesso.",
            documento.Id,
            documento.Numero,
            expedicao.Id));
    }
}

using System.Net.Http.Json;
using System.Text.Json;

namespace PagamentoService.Integrations;

public record ProdutoEstoqueResponse(
    int Id,
    string Nome,
    string Descricao,
    decimal Preco,
    int Quantidade,
    bool Ativo);

public record ItemBaixaRequest(int ProdutoId, int Quantidade);

public record BaixaEstoqueRequest(
    int VendaId,
    string ClienteNome,
    string ClienteEmail,
    string? EnderecoEntrega,
    List<ItemBaixaRequest> Itens);

public record BaixaEstoqueResponse(
    bool Sucesso,
    string Mensagem,
    int? DocumentoFiscalId = null,
    string? NumeroDocumentoFiscal = null,
    int? ExpedicaoId = null);

/// <summary>
/// Cliente HTTP do EstoqueService.
/// 1) Busca simples: consulta os dados do produto ao registrar a venda.
/// 2) Alteração de dados: dispara a baixa de estoque ao confirmar o pagamento.
/// </summary>
public class EstoqueApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EstoqueApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EstoqueApiClient(HttpClient http, ILogger<EstoqueApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Busca simples de dados: Pagamento -> Estoque.</summary>
    public async Task<ProdutoEstoqueResponse?> ObterProdutoAsync(int produtoId)
    {
        try
        {
            var response = await _http.GetAsync($"api/produtos/{produtoId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProdutoEstoqueResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar o produto {ProdutoId} no EstoqueService.", produtoId);
            return null;
        }
    }

    /// <summary>Alteração de dados: Pagamento -> Estoque (baixa de estoque).</summary>
    public async Task<BaixaEstoqueResponse?> RegistrarBaixaAsync(BaixaEstoqueRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/estoque/baixa", request);
            var resultado = await response.Content.ReadFromJsonAsync<BaixaEstoqueResponse>(JsonOptions);

            if (resultado is null)
                _logger.LogWarning("EstoqueService respondeu {StatusCode} sem corpo válido.", (int)response.StatusCode);

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar a baixa de estoque no EstoqueService.");
            return null;
        }
    }
}

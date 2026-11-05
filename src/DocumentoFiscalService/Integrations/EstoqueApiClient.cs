using System.Text.Json;

namespace DocumentoFiscalService.Integrations;

public record ProdutoEstoqueResponse(
    int Id,
    string Nome,
    string Descricao,
    decimal Preco,
    int Quantidade,
    bool Ativo);

/// <summary>
/// Cliente HTTP do EstoqueService.
/// Busca simples: ao emitir o documento fiscal, consulta os dados do produto no estoque.
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
}

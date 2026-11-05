using System.Net.Http.Json;
using System.Text.Json;

namespace EstoqueService.Integrations;

public record ItemExpedicaoRequest(int ProdutoId, string ProdutoNome, int Quantidade, decimal PrecoUnitario);

public record CriarExpedicaoRequest(
    int DocumentoFiscalId,
    string NumeroDocumentoFiscal,
    int VendaId,
    string ClienteNome,
    string? EnderecoEntrega,
    List<ItemExpedicaoRequest> Itens);

public record ExpedicaoCriadaResponse(
    int Id,
    int DocumentoFiscalId,
    string NumeroDocumentoFiscal,
    int VendaId,
    string ClienteNome,
    string? EnderecoEntrega,
    List<ItemExpedicaoResponse> Itens,
    string Status,
    DateTime CriadaEm);

public record ItemExpedicaoResponse(int ProdutoId, string ProdutoNome, int Quantidade, decimal PrecoUnitario);

/// <summary>
/// Cliente HTTP do ExpedicaoService.
/// Após o documento fiscal ser emitido, o estoque dispara a criação da expedição (alteração de dados).
/// </summary>
public class ExpedicaoApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ExpedicaoApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ExpedicaoApiClient(HttpClient http, ILogger<ExpedicaoApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ExpedicaoCriadaResponse?> CriarAsync(CriarExpedicaoRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/expedicoes", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ExpedicaoService retornou {StatusCode} ao criar expedição.",
                    (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ExpedicaoCriadaResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na integração com o ExpedicaoService.");
            return null;
        }
    }
}

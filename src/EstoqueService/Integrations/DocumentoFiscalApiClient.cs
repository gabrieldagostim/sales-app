using System.Net.Http.Json;
using System.Text.Json;

namespace EstoqueService.Integrations;

public record EmitirDocumentoFiscalRequest(
    int VendaId,
    string ClienteNome,
    string ClienteEmail,
    List<ItemDocumentoFiscalRequest> Itens);

public record ItemDocumentoFiscalRequest(int ProdutoId, int Quantidade);

public record DocumentoFiscalEmitidoResponse(
    int Id,
    string Numero,
    int VendaId,
    string ClienteNome,
    string ClienteEmail,
    List<ItemDocumentoFiscalEmitidoResponse> Itens,
    decimal ValorTotal,
    string Status,
    DateTime EmitidoEm);

public record ItemDocumentoFiscalEmitidoResponse(int ProdutoId, string ProdutoNome, int Quantidade, decimal PrecoUnitario);

public record ErroResponse(string? Mensagem);

/// <summary>
/// Cliente HTTP do DocumentoFiscalService.
/// A baixa de estoque dispara a emissão do documento fiscal (alteração de dados).
/// </summary>
public class DocumentoFiscalApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<DocumentoFiscalApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DocumentoFiscalApiClient(HttpClient http, ILogger<DocumentoFiscalApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<DocumentoFiscalEmitidoResponse?> EmitirAsync(EmitirDocumentoFiscalRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/documentos-fiscais/emitir", request);

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadFromJsonAsync<ErroResponse>(JsonOptions);
                _logger.LogWarning("DocumentoFiscalService retornou {StatusCode}: {Mensagem}",
                    (int)response.StatusCode, erro?.Mensagem);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DocumentoFiscalEmitidoResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na integração com o DocumentoFiscalService.");
            return null;
        }
    }
}

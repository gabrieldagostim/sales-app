namespace PagamentoService.Dtos;

public record ItemVendaRequest(int ProdutoId, int Quantidade);

public record CriarVendaRequest(
    string ClienteNome,
    string ClienteEmail,
    string? EnderecoEntrega,
    List<ItemVendaRequest> Itens);

public record VendaResponse(
    int Id,
    string ClienteNome,
    string ClienteEmail,
    string? EnderecoEntrega,
    List<ItemVendaResponse> Itens,
    decimal ValorTotal,
    string Status,
    DateTime CriadaEm);

public record ItemVendaResponse(
    int ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

public record PagarVendaResponse(
    bool Sucesso,
    string Mensagem,
    string Status,
    int? DocumentoFiscalId = null,
    string? NumeroDocumentoFiscal = null,
    int? ExpedicaoId = null);

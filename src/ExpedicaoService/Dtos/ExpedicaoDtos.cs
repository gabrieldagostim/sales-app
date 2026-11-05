namespace ExpedicaoService.Dtos;

public record ItemExpedicaoRequest(
    int ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario);

public record CriarExpedicaoRequest(
    int DocumentoFiscalId,
    string NumeroDocumentoFiscal,
    int VendaId,
    string ClienteNome,
    string? EnderecoEntrega,
    List<ItemExpedicaoRequest> Itens);

public record ExpedicaoResponse(
    int Id,
    int DocumentoFiscalId,
    string NumeroDocumentoFiscal,
    int VendaId,
    string ClienteNome,
    string? EnderecoEntrega,
    List<ItemExpedicaoResponse> Itens,
    string Status,
    DateTime CriadaEm);

public record ItemExpedicaoResponse(
    int ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario);

public record AtualizarStatusRequest(string Status);

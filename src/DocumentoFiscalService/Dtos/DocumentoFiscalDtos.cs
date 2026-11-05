namespace DocumentoFiscalService.Dtos;

public record ItemDocumentoRequest(int ProdutoId, int Quantidade);

public record EmitirDocumentoRequest(
    int VendaId,
    string ClienteNome,
    string ClienteEmail,
    List<ItemDocumentoRequest> Itens);

public record DocumentoFiscalResponse(
    int Id,
    string Numero,
    int VendaId,
    string ClienteNome,
    string ClienteEmail,
    List<ItemDocumentoFiscalResponse> Itens,
    decimal ValorTotal,
    string Status,
    DateTime EmitidoEm);

public record ItemDocumentoFiscalResponse(
    int ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

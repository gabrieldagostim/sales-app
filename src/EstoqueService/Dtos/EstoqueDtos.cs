namespace EstoqueService.Dtos;

public record ProdutoResponse(int Id, string Nome, string Descricao, decimal Preco, int Quantidade, bool Ativo);

public record CriarProdutoRequest(string Nome, string Descricao, decimal Preco, int Quantidade);

public record AtualizarProdutoRequest(string Nome, string Descricao, decimal Preco, int Quantidade, bool Ativo);

public record ItemBaixaRequest(int ProdutoId, int Quantidade);

/// <summary>
/// Payload enviado pelo PagamentoService ao confirmar um pagamento.
/// </summary>
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

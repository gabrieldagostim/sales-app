using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ExpedicaoService.Models;

/// <summary>
/// Expedição criada após a emissão do documento fiscal.
/// Os itens são persistidos como JSON em uma coluna TEXT do SQLite.
/// </summary>
public class Expedicao
{
    public int Id { get; set; }

    public int DocumentoFiscalId { get; set; }
    public string NumeroDocumentoFiscal { get; set; } = string.Empty;

    public int VendaId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string? EnderecoEntrega { get; set; }

    public string ItensJson { get; set; } = "[]";

    /// <summary>Preparando | Enviada | Entregue | Cancelada</summary>
    public string Status { get; set; } = "Preparando";

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public List<ItemExpedicao> Itens
    {
        get => JsonSerializer.Deserialize<List<ItemExpedicao>>(ItensJson) ?? new List<ItemExpedicao>();
        set => ItensJson = JsonSerializer.Serialize(value);
    }
}

public class ItemExpedicao
{
    public int ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

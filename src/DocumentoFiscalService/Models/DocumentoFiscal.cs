using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DocumentoFiscalService.Models;

/// <summary>
/// Documento fiscal emitido para uma venda.
/// Os itens são persistidos como JSON em uma coluna TEXT do SQLite.
/// </summary>
public class DocumentoFiscal
{
    public int Id { get; set; }

    /// <summary>Ex.: NF-2026-000001</summary>
    public string Numero { get; set; } = string.Empty;

    public int VendaId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;

    public string ItensJson { get; set; } = "[]";

    public decimal ValorTotal { get; set; }

    public string Status { get; set; } = "Emitido";

    public DateTime EmitidoEm { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public List<ItemDocumentoFiscal> Itens
    {
        get => JsonSerializer.Deserialize<List<ItemDocumentoFiscal>>(ItensJson) ?? new List<ItemDocumentoFiscal>();
        set => ItensJson = JsonSerializer.Serialize(value);
    }
}

public class ItemDocumentoFiscal
{
    public int ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => Quantidade * PrecoUnitario;
}

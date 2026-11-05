using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PagamentoService.Models;

/// <summary>
/// Venda registrada no microsserviço de pagamento.
/// Os itens são persistidos como JSON em uma coluna TEXT do SQLite.
/// </summary>
public class Venda
{
    public int Id { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public string? EnderecoEntrega { get; set; }

    public string ItensJson { get; set; } = "[]";

    public decimal ValorTotal { get; set; }

    /// <summary>Pendente | Pago | Falha</summary>
    public string Status { get; set; } = "Pendente";

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public List<ItemVenda> Itens
    {
        get => JsonSerializer.Deserialize<List<ItemVenda>>(ItensJson) ?? new List<ItemVenda>();
        set => ItensJson = JsonSerializer.Serialize(value);
    }
}

public class ItemVenda
{
    public int ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => Quantidade * PrecoUnitario;
}

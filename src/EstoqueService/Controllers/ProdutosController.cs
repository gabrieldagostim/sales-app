using EstoqueService.Data;
using EstoqueService.Dtos;
using EstoqueService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly EstoqueDbContext _db;

    public ProdutosController(EstoqueDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista todos os produtos do catálogo.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoResponse>>> Listar()
    {
        var produtos = await _db.Produtos.AsNoTracking().ToListAsync();
        return Ok(produtos.Select(ToResponse));
    }

    /// <summary>Retorna um produto pelo id (usado pelas integrações).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoResponse>> Obter(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null)
            return NotFound(new { mensagem = $"Produto {id} não encontrado." });

        return Ok(ToResponse(produto));
    }

    /// <summary>Cadastra um novo produto.</summary>
    [HttpPost]
    public async Task<ActionResult<ProdutoResponse>> Criar(CriarProdutoRequest request)
    {
        var produto = new Produto
        {
            Nome = request.Nome,
            Descricao = request.Descricao,
            Preco = request.Preco,
            Quantidade = request.Quantidade
        };

        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, ToResponse(produto));
    }

    /// <summary>Atualiza os dados de um produto.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, AtualizarProdutoRequest request)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null)
            return NotFound(new { mensagem = $"Produto {id} não encontrado." });

        produto.Nome = request.Nome;
        produto.Descricao = request.Descricao;
        produto.Preco = request.Preco;
        produto.Quantidade = request.Quantidade;
        produto.Ativo = request.Ativo;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(produto));
    }

    /// <summary>Inativa um produto (exclusão lógica).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null)
            return NotFound(new { mensagem = $"Produto {id} não encontrado." });

        produto.Ativo = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static ProdutoResponse ToResponse(Produto p) =>
        new(p.Id, p.Nome, p.Descricao, p.Preco, p.Quantidade, p.Ativo);
}

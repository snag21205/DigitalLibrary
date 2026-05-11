using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using BookService.Models;

namespace BookService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly string _connectionString;

    public BooksController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("MariaDBConnection")
            ?? "Server=localhost;Port=3306;Database=BookDB;Uid=root;Pwd=123456;";
    }

    [HttpGet]
    public async Task<IActionResult> GetAllBooks()
    {
        using var connection = new MySqlConnection(_connectionString);
        var books = await connection.QueryAsync<Book>("SELECT Id, Title, Author, Stock FROM Books");
        return Ok(books);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBook(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        var book = await connection.QueryFirstOrDefaultAsync<Book>(
            "SELECT Id, Title, Author, Stock FROM Books WHERE Id = @Id",
            new { Id = id });

        if (book == null) return NotFound();
        return Ok(book);
    }

    [HttpPut("{id}/reduce-stock")]
    public async Task<IActionResult> ReduceStock(int id)
    {
        using var connection = new MySqlConnection(_connectionString);

        // Kiểm tra stock hiện tại
        var book = await connection.QueryFirstOrDefaultAsync<Book>(
            "SELECT Id, Stock FROM Books WHERE Id = @Id", new { Id = id });

        if (book == null) return NotFound(new { Message = "Sách không tồn tại" });
        if (book.Stock <= 0) return BadRequest(new { Message = "Sách đã hết" });

        // Giảm stock
        await connection.ExecuteAsync(
            "UPDATE Books SET Stock = Stock - 1 WHERE Id = @Id", new { Id = id });

        return Ok(new { Message = "Cập nhật stock thành công", NewStock = book.Stock - 1 });
    }

    [HttpPut("{id}/increase-stock")]
    public async Task<IActionResult> IncreaseStock(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "UPDATE Books SET Stock = Stock + 1 WHERE Id = @Id", new { Id = id });

        return Ok(new { Message = "Trả sách thành công" });
    }
}
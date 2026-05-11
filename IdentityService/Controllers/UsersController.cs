using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using IdentityService.Models;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly string _connectionString;

    public UsersController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("MariaDBConnection")
            ?? "Server=localhost;Port=3306;Database=IdentityDB;Uid=root;Pwd=123456;";
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT Id, Username, FullName, UserRank FROM Users WHERE Id = @Id",
            new { Id = id });

        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet("can-borrow/{userId}")]
    public async Task<IActionResult> CanBorrow(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);

        // Lấy thông tin user
        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT Id, UserRank FROM Users WHERE Id = @Id",
            new { Id = userId });

        if (user == null) return Ok(new { CanBorrow = false, Message = "User không tồn tại" });

        // Đếm số sách đang mượn (cần gọi từ Borrowing Service, nhưng tạm thời)
        // Ở đây, Identity chỉ trả về rank, Borrowing Service sẽ tự tính toán
        int maxBorrow = user.UserRank == "Gold" ? 5 : 3;

        return Ok(new { CanBorrow = true, UserRank = user.UserRank, MaxBorrow = maxBorrow });
    }
}
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using BorrowingService.Models;
using System.Text.Json;

namespace BorrowingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BorrowController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _clientFactory;
    private readonly string _connectionString;

    public BorrowController(IConfiguration config, IHttpClientFactory clientFactory)
    {
        _config = config;
        _clientFactory = clientFactory;
        _connectionString = _config.GetConnectionString("MariaDBConnection")
            ?? "Server=localhost;Port=3306;Database=BorrowingDB;Uid=root;Pwd=123456;";
    }

    // POST: api/borrow?userId=1&bookId=101
    [HttpPost]
    public async Task<IActionResult> BorrowBook(int userId, int bookId)
    {
        var identityUrl = _config["ServiceUrls:IdentityService"];
        var bookUrl = _config["ServiceUrls:BookService"];
        var notificationUrl = _config["ServiceUrls:NotificationService"];

        var client = _clientFactory.CreateClient();

        // Bước 1: Kiểm tra User và hạng
        UserInfo? user = null;
        try
        {
            var userResponse = await client.GetAsync($"{identityUrl}/api/users/{userId}");
            if (!userResponse.IsSuccessStatusCode)
            {
                return BadRequest(new { Error = "User không tồn tại" });
            }
            var userJson = await userResponse.Content.ReadAsStringAsync();
            user = JsonSerializer.Deserialize<UserInfo>(userJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = $"Lỗi khi gọi Identity Service: {ex.Message}" });
        }

        // Bước 2: Kiểm tra sách còn không
        BookInfo? book = null;
        try
        {
            var bookResponse = await client.GetAsync($"{bookUrl}/api/books/{bookId}");
            if (!bookResponse.IsSuccessStatusCode)
            {
                return BadRequest(new { Error = "Sách không tồn tại" });
            }
            var bookJson = await bookResponse.Content.ReadAsStringAsync();
            book = JsonSerializer.Deserialize<BookInfo>(bookJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (book.Stock <= 0)
            {
                return BadRequest(new { Error = "Sách đã hết, không thể mượn" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = $"Lỗi khi gọi Book Service: {ex.Message}" });
        }

        // Bước 3: Kiểm tra hạn mức mượn của user
        int maxBorrow = user!.UserRank == "Gold" ? 5 : 3;

        using var connection = new MySqlConnection(_connectionString);
        var currentBorrowCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM BorrowRecords WHERE UserId = @UserId AND Status = 'BORROWED'",
            new { UserId = userId });

        if (currentBorrowCount >= maxBorrow)
        {
            return BadRequest(new { Error = $"Bạn đã mượn {currentBorrowCount}/{maxBorrow} sách. Không thể mượn thêm!" });
        }

        // Bước 4: Lưu phiếu mượn
        var borrowId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO BorrowRecords (UserId, BookId, BorrowDate, Status) 
              VALUES (@UserId, @BookId, NOW(), 'BORROWED');
              SELECT LAST_INSERT_ID();",
            new { UserId = userId, BookId = bookId });

        // Bước 5: Giảm stock sách
        try
        {
            await client.PutAsync($"{bookUrl}/api/books/{bookId}/reduce-stock", null);
        }
        catch (Exception ex)
        {
            // Log lỗi nhưng không rollback (ghi nhận để xử lý sau)
            Console.WriteLine($"Warning: Không thể cập nhật stock - {ex.Message}");
        }

        // Bước 6: Gửi thông báo
        var message = $"[MƯỢN SÁCH] Bạn đã mượn sách '{book!.Title}' thành công! Hạn mức còn lại: {maxBorrow - currentBorrowCount - 1} sách.";
        try
        {
            await client.PostAsync($"{notificationUrl}/api/notifications?userId={userId}&message={Uri.EscapeDataString(message)}", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Không thể gửi thông báo - {ex.Message}");
        }

        return Ok(new
        {
            BorrowId = borrowId,
            Message = $"Mượn sách thành công! Sách: {book.Title}. Hạn mức còn lại: {maxBorrow - currentBorrowCount - 1} sách."
        });
    }

    // POST: api/borrow/return?borrowId=1
    [HttpPost("return")]
    public async Task<IActionResult> ReturnBook(int borrowId)
    {
        var bookUrl = _config["ServiceUrls:BookService"];
        var client = _clientFactory.CreateClient();

        using var connection = new MySqlConnection(_connectionString);

        // Lấy thông tin phiếu mượn
        var record = await connection.QueryFirstOrDefaultAsync<BorrowRecord>(
            "SELECT Id, UserId, BookId, Status FROM BorrowRecords WHERE Id = @Id",
            new { Id = borrowId });

        if (record == null) return NotFound(new { Error = "Không tìm thấy phiếu mượn" });
        if (record.Status == "RETURNED") return BadRequest(new { Error = "Sách đã được trả rồi" });

        // Cập nhật phiếu mượn
        await connection.ExecuteAsync(
            @"UPDATE BorrowRecords SET ReturnDate = NOW(), Status = 'RETURNED' WHERE Id = @Id",
            new { Id = borrowId });

        // Tăng stock sách
        await client.PutAsync($"{bookUrl}/api/books/{record.BookId}/increase-stock", null);

        return Ok(new { Message = "Trả sách thành công!" });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserBorrowHistory(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var records = await connection.QueryAsync<BorrowRecord>(
            "SELECT Id, UserId, BookId, BorrowDate, ReturnDate, Status FROM BorrowRecords WHERE UserId = @UserId ORDER BY BorrowDate DESC",
            new { UserId = userId });

        return Ok(records);
    }
}
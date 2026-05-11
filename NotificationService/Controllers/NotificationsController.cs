using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using NotificationService.Models;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly string _connectionString;

    public NotificationsController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("MariaDBConnection")
            ?? "Server=localhost;Port=3306;Database=NotificationDB;Uid=root;Pwd=123456;";
    }

    [HttpPost]
    public async Task<IActionResult> SendNotification(int userId, string message)
    {
        using var connection = new MySqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO Notifications (UserId, Message, CreatedAt) 
              VALUES (@UserId, @Message, NOW());
              SELECT LAST_INSERT_ID();",
            new { UserId = userId, Message = message });

        // Giả lập gửi email/SMS (chỉ log ra console)
        Console.WriteLine($"[NOTIFICATION] User {userId}: {message}");

        return Ok(new { NotificationId = id, Message = "Đã gửi thông báo" });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var notifications = await connection.QueryAsync<Notification>(
            "SELECT Id, UserId, Message, CreatedAt FROM Notifications WHERE UserId = @UserId ORDER BY CreatedAt DESC",
            new { UserId = userId });

        return Ok(notifications);
    }
}
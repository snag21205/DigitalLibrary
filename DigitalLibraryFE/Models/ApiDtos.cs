namespace DigitalLibraryFE.Models
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int Stock { get; set; }
    }

    public class BorrowRecordDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserRank { get; set; } = string.Empty;
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class BorrowResultDto
    {
        public int BorrowId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class NotificationResultDto
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CanBorrowDto
    {
        public bool CanBorrow { get; set; }
        public string UserRank { get; set; } = string.Empty;
        public int MaxBorrow { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

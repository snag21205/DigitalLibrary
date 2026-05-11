namespace BorrowingService.Models
{
    public class BorrowRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = "BORROWED";
    }
    
    public class UserInfo
    {
        public int Id { get; set; }
        public string UserRank { get; set; } = string.Empty;
    }

    public class BookInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}

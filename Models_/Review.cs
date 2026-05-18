using System;

namespace Read_Write_Slowly.Models_
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public string Text { get; set; }
        public double Rating { get; set; }
        public bool IsFrozen { get; set; }
        public DateTime CreatedAt { get; set; }
        public string BookTitle { get; set; }
        public string UserDisplayName { get; set; } // Имя автора отзыва для UI
    }
}
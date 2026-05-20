using System.Collections.Generic;

namespace Read_Write_Slowly.Models_
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverPath { get; set; } // Путь к картинке (локальный или URL)
        public int IsFrozen { get; set; }
        public string ContentText { get; set; }
        public int AuthorUserId { get; set; }
    }
}

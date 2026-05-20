using System.Collections.Generic;

namespace Read_Write_Slowly
{
    public partial class Book
    {
        // Вычисляемые/JOIN-поля для UI — не хранятся в БД
        public string AuthorName { get; set; }
        public double AverageRating { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
    }
}
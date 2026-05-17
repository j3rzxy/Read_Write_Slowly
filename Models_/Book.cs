using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read_Write_Slowly.Models_
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverPath { get; set; } // Путь к картинке (локальный или URL)
        public string AuthorName { get; set; } // Имя автора из связанной таблицы/пользователя
        public double AverageRating { get; set; } // Средняя оценка
        public bool IsFrozen { get; set; }
    }
}

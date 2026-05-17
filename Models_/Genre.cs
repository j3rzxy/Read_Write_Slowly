using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read_Write_Slowly.Models_
{
    public class Genre
    {
        public int GenreId { get; set; }
        public string Name { get; set; }

        // Переопределяем ToString, чтобы жанры красиво отображались в ComboBox фильтрации
        public override string ToString() => Name;
    }
}

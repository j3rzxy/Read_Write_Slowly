using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read_Write_Slowly.Models_
{
    public class ComplaintInfo
    {
        public int ComplaintId { get; set; }
        public string ReporterName { get; set; }  // Кто пожаловался
        public string TargetType { get; set; }    // 'book' или 'review'
        public int TargetId { get; set; }
        public string TargetDescription { get; set; } // Название книги или кусочек текста отзыва
        public string Reason { get; set; }
        public string CreatedAt { get; set; }
    }
}

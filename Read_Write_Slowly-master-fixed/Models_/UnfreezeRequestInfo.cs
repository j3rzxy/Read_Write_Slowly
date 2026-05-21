using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read_Write_Slowly.Models_
{
    public class UnfreezeRequestInfo
    {
        public int UnfreezeRequestId { get; set; }
        public int UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string TargetType { get; set; } // 'account' или 'book'
        public int? TargetId { get; set; }     // Будет Null для аккаунта, или ID книги
        public string TargetName { get; set; }   // "Весь аккаунт" или Название книги
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
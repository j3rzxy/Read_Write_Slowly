using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read_Write_Slowly.Models_
{
    public class User
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public string DisplayName { get; set; }
        public int RoleId { get; set; }
        public bool IsFrozen { get; set; }

        public bool IsAdmin => RoleId == 3;
        public bool IsAuthor => RoleId == 2;
    }
}

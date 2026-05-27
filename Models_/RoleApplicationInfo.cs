using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read_Write_Slowly.Models_
{
    public class RoleApplicationInfo
    {
        public int RoleApplicationId { get; set; }
        public int UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string UserLogin { get; set; }
        public int RequestedRoleId { get; set; }
        public string RequestedRoleName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

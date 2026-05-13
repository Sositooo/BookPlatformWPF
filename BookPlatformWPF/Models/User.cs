using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookPlatformWPF.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Login { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public bool IsFrozen { get; set; }
        public string FreezeReason { get; set; }
    }
}
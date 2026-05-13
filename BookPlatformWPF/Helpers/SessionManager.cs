using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookPlatformWPF.Helpers
{
    public static class SessionManager
    {
        public static Users CurrentUser { get; set; } // EF-сущность из модели

        // Удобные свойства
        public static int UserID => CurrentUser?.UserID ?? 0;
        public static string Login => CurrentUser?.Login;
        public static string DisplayName => CurrentUser?.DisplayName;
        public static string Email => CurrentUser?.Email;
        public static int RoleID => CurrentUser?.RoleID ?? 0;
        public static bool IsFrozen => CurrentUser?.IsFrozen ?? false;
        public static string FreezeReason => CurrentUser?.FreezeReason;

        public static bool IsAdmin => RoleID == 3;
        public static bool IsAuthor => RoleID == 2;

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}

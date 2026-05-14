using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace BookPlatformWPF.Helpers
{
    public static class Core
    {
        // Единственная точка доступа к контексту EF
        private static BookPlatformEntities1 _context;

        public static BookPlatformEntities1 DB
        {
            get
            {
                if (_context == null)
                    _context = new BookPlatformEntities1();
                return _context;
            }
        }

        // Пересоздать контекст 
        public static void Reset()
        {
            _context?.Dispose();
            _context = new BookPlatformEntities1();
        }
    }
}

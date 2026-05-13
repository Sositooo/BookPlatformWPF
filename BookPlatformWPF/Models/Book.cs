using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookPlatformWPF.Models
{
    public class Book
    {
        public int BookID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public int AuthorID { get; set; }
        public string AuthorName { get; set; }
        public bool IsFrozen { get; set; }
        public string FreezeReason { get; set; }
        public string CoverPath { get; set; }
        public double AvgRating { get; set; }
        public string Genres { get; set; }
    }
}

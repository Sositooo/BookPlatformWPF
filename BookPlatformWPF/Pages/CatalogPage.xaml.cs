using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF.Pages
{
    public partial class CatalogPage : Page
    {
        public CatalogPage()
        {
            InitializeComponent();
            LoadGenresFilter();
            LoadBooks();
        }

        private void LoadGenresFilter()
        {
            var genres = Core.DB.Genres.OrderBy(g => g.Name).ToList();
            // Добавляем "Все жанры" в начало
            CmbGenre.ItemsSource = new[] { new Models.Genre { GenreID = 0, Name = "Все жанры" } }
                .Concat(genres.Select(g => new Models.Genre { GenreID = g.GenreID, Name = g.Name }))
                .ToList();
            CmbGenre.SelectedIndex = 0;
        }

        private void LoadBooks()
        {
            // Грузим незамороженные книги с авторами
            var books = Core.DB.Books
                .Include("Users")           // автор
                .Include("Reviews")         // для рейтинга
                .Include("Genres")          // жанры
                .Where(b => !b.IsFrozen)
                .ToList();

            // Сохраняем в _allBooks и вызываем ApplyFilters
            _allBooks = books;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string search = TxtSearch?.Text?.ToLower() ?? "";
            int genreID = (CmbGenre?.SelectedValue is int g) ? g : 0;
            bool sortRating = (CmbSort?.SelectedIndex ?? 0) == 1;

            var filtered = _allBooks.Where(b =>
                (string.IsNullOrEmpty(search) ||
                 b.Title.ToLower().Contains(search) ||
                 b.Users.DisplayName.ToLower().Contains(search)) &&
                (genreID == 0 || b.Genres.Any(genre => genre.GenreID == genreID))
            );

            filtered = sortRating
                ? filtered.OrderByDescending(b =>
                    b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0)
                : filtered.OrderBy(b => b.Title);

            BooksPanel.Children.Clear();
            foreach (var book in filtered)
            {
                double avg = book.Reviews.Any()
                    ? book.Reviews.Average(r => (double)r.Rating) : 0;
                string genres = string.Join(", ", book.Genres.Select(g => g.Name));
                BooksPanel.Children.Add(CreateBookCard(
                    book.BookID, book.Title,
                    book.Users.DisplayName, avg));
            }
        }

        // Поля и CreateBookCard, AddToList — остаются те же, только:
        // AddToList через EF:
        private void AddToList(int bookId, string section)
        {
            var existing = Core.DB.ReadingLists
                .FirstOrDefault(r => r.UserID == SessionManager.UserID && r.BookID == bookId);

            if (existing != null)
                existing.Section = section;
            else
                Core.DB.ReadingLists.Add(new ReadingLists
                {
                    UserID = SessionManager.UserID,
                    BookID = bookId,
                    Section = section
                });

            Core.DB.SaveChanges();
            MessageBox.Show($"Книга добавлена в «{section}»", "Готово");
        }
    }
}
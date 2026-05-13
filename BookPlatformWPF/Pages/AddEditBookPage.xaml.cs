using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF.Pages
{
    public partial class AddEditBookPage : Page
    {
        private readonly int? _bookId; // null = добавление, число = редактирование
        private Books _book;   // EF-объект для редактирования

        public AddEditBookPage(int? bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            LoadGenres();

            if (_bookId.HasValue)
            {
                TxtPageTitle.Text = "Редактировать книгу";
                LoadBook();
            }
        }

        private void LoadGenres()
        {
            // Загружаем EF-объекты жанров — ListBox покажет Name через DisplayMemberPath
            var genres = Core.DB.Genres.OrderBy(g => g.Name).ToList();
            LstGenres.ItemsSource = genres;
        }

        private void LoadBook()
        {
            // Include("Genres") нужен чтобы отметить текущие жанры книги
            _book = Core.DB.Books
                .Include(b => b.Genres)
                .FirstOrDefault(b => b.BookID == _bookId.Value);

            if (_book == null) return;

            TxtTitle.Text = _book.Title;
            TxtDesc.Text = _book.Description;
            TxtContent.Text = _book.Content;

            // Отмечаем в ListBox жанры которые уже есть у книги
            foreach (Genres item in LstGenres.Items)
                if (_book.Genres.Any(g => g.GenreID == item.GenreID))
                    LstGenres.SelectedItems.Add(item);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTitle.Text))
            {
                LblError.Text = "Название обязательно.";
                return;
            }

            if (_bookId.HasValue)
            {
                // ── РЕЖИМ РЕДАКТИРОВАНИЯ ──────────────────────────
                // _book уже загружен — просто меняем поля
                _book.Title = TxtTitle.Text.Trim();
                _book.Description = TxtDesc.Text.Trim();
                _book.Content = TxtContent.Text.Trim();

                // Очищаем старые жанры и добавляем выбранные
                // _book.Genres — это навигационная коллекция EF (Many-to-Many через BookGenres)
                _book.Genres.Clear();
                foreach (Genres g in LstGenres.SelectedItems)
                {
                    // Find нужен чтобы получить объект ИЗ контекста EF
                    // нельзя добавлять объект из другого контекста
                    var genre = Core.DB.Genres.Find(g.GenreID);
                    if (genre != null) _book.Genres.Add(genre);
                }
            }
            else
            {
                // ── РЕЖИМ ДОБАВЛЕНИЯ ─────────────────────────────
                var newBook = new Books
                {
                    Title = TxtTitle.Text.Trim(),
                    Description = TxtDesc.Text.Trim(),
                    Content = TxtContent.Text.Trim(),
                    AuthorID = SessionManager.UserID,
                    IsFrozen = false
                };

                foreach (Genres g in LstGenres.SelectedItems)
                {
                    var genre = Core.DB.Genres.Find(g.GenreID);
                    if (genre != null) newBook.Genres.Add(genre);
                }

                Core.DB.Books.Add(newBook);
            }

            // Одна точка сохранения для обоих режимов
            Core.DB.SaveChanges();
            Core.Reset();

            MessageBox.Show("Книга сохранена!", "Готово");
            MainWindow.Instance.Navigate(new AuthorPage());
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => MainWindow.Instance.Navigate(new AuthorPage());
    }
}
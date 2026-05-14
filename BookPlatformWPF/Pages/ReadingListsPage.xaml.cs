using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF.Pages
{
    public partial class ReadingListsPage : Page
    {
        private bool _isLoaded = false;
        private string _section = "Читаю";
        private List<ReadingLists> _items = new List<ReadingLists>();

        public ReadingListsPage()
        {
            InitializeComponent();
            LoadGenres();
            LoadBooks();
            _isLoaded = true;
        }

        private void LoadGenres()
        {
            var dbGenres = Core.DB.Genres.OrderBy(g => g.Name).ToList();
            var list = new List<object>();
            list.Add(new { GenreID = 0, Name = "Все жанры" });
            foreach (var g in dbGenres)
                list.Add(new { GenreID = g.GenreID, Name = g.Name });

            CmbGenre.ItemsSource = list;
            CmbGenre.DisplayMemberPath = "Name";
            CmbGenre.SelectedValuePath = "GenreID";
            CmbGenre.SelectedIndex = 0;
        }

        private void BtnTab_Click(object sender, RoutedEventArgs e)
        {
            _section = (sender as Button)?.Tag?.ToString() ?? "Читаю";
            LoadBooks();
        }

        private void Filter_Changed(object sender, System.EventArgs e)
            => ApplyFilters();

        private void LoadBooks()
        {
            // Загружаем записи ReadingLists текущего пользователя
            // с подгрузкой книги, автора книги, отзывов и жанров
            _items = Core.DB.ReadingLists
                .Include(rl => rl.Books)
                .Include(rl => rl.Books.Users)
                .Include(rl => rl.Books.Reviews)
                .Include(rl => rl.Books.Genres)
                .Where(rl => rl.UserID == SessionManager.UserID &&
                             rl.Section == _section)
                .ToList();

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (!_isLoaded) return;       
            if (BooksPanel == null) return;

            string search = TxtSearch?.Text?.ToLower() ?? "";
            int genreID = 0;
            if (CmbGenre?.SelectedValue != null)
                int.TryParse(CmbGenre.SelectedValue.ToString(), out genreID);

            IEnumerable<ReadingLists> filtered = _items.Where(rl =>
                (string.IsNullOrEmpty(search) ||
                 rl.Books.Title.ToLower().Contains(search) ||
                 rl.Books.Users.DisplayName.ToLower().Contains(search)) &&
                (genreID == 0 || rl.Books.Genres.Any(g => g.GenreID == genreID))
            );

            bool byRating = (CmbSort?.SelectedIndex ?? 0) == 1;
            filtered = byRating
                ? filtered.OrderByDescending(rl =>
                    rl.Books.Reviews.Any()
                        ? rl.Books.Reviews.Average(r => (double)r.Rating) : 0)
                : filtered.OrderBy(rl => rl.Books.Title);

            BooksPanel.Children.Clear();
            foreach (var item in filtered)
                BooksPanel.Children.Add(CreateCard(item));
        }

        private Border CreateCard(ReadingLists item)
        {
            var book = item.Books;
            double avg = book.Reviews.Any()
                ? book.Reviews.Average(r => (double)r.Rating) : 0;

            var border = new Border
            {
                Width = 170,
                Height = 255,
                Margin = new Thickness(6),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6)
            };

            var sp = new StackPanel { Margin = new Thickness(8) };
            sp.Children.Add(new TextBlock
            {
                Text = book.Title,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 40
            });
            sp.Children.Add(new TextBlock
            {
                Text = book.Users?.DisplayName ?? "—",
                Foreground = Brushes.Gray,
                FontSize = 11
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"⭐ {avg:F1}",
                FontSize = 11,
                Margin = new Thickness(0, 3, 0, 6)
            });

            int bookId = book.BookID;

            var btnRead = new Button
            {
                Content = "Читать",
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 0, 5),
                Background = new SolidColorBrush(Color.FromRgb(74, 144, 217)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnRead.Click += (s, e) => MainWindow.Instance.Navigate(new BookPage(bookId));

            // Выбор нового раздела для перемещения
            var cmbMove = new ComboBox { Margin = new Thickness(0, 0, 0, 4) };
            cmbMove.Items.Add("Читаю");
            cmbMove.Items.Add("Прочитано");
            cmbMove.Items.Add("В планах");
            cmbMove.Items.Add("Заброшено");
            cmbMove.SelectedItem = _section;

            var btnMove = new Button
            {
                Content = "Переместить",
                Padding = new Thickness(6, 3, 6, 3),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                Foreground = Brushes.White
            };
            btnMove.Click += (s, e) =>
            {
                string newSection = cmbMove.SelectedItem?.ToString();
                if (newSection == null || newSection == _section) return;
                MoveBook(bookId, newSection);
            };

            sp.Children.Add(btnRead);
            sp.Children.Add(cmbMove);
            sp.Children.Add(btnMove);
            border.Child = sp;
            return border;
        }

        private void MoveBook(int bookId, string newSection)
        {
            // Находим запись и просто меняем Section
            var entry = Core.DB.ReadingLists
                .FirstOrDefault(rl => rl.UserID == SessionManager.UserID
                                   && rl.BookID == bookId);
            if (entry == null) return;

            entry.Section = newSection;
            Core.DB.SaveChanges();
            Core.Reset();
            LoadBooks(); // перерисовываем текущий список
        }
    }
}
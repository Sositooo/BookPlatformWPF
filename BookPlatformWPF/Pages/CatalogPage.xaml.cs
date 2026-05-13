using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF.Pages
{
    public partial class CatalogPage : Page
    {
        // Все книги загружаем один раз — фильтруем уже локально
        private List<Books> _allBooks = new List<Books>();

        public CatalogPage()
        {
            InitializeComponent();
            LoadGenres();
            LoadBooks();
        }

        // Заполняем ComboBox жанров
        private void LoadGenres()
        {
            // Берём жанры через EF
            var dbGenres = Core.DB.Genres.OrderBy(g => g.Name).ToList();

            // Строим список: сначала "Все жанры" с ID=0, потом реальные жанры
            var list = new List<object>();
            list.Add(new { GenreID = 0, Name = "Все жанры" });
            foreach (var g in dbGenres)
                list.Add(new { GenreID = g.GenreID, Name = g.Name });

            CmbGenre.ItemsSource = list;
            CmbGenre.DisplayMemberPath = "Name";
            CmbGenre.SelectedValuePath = "GenreID";
            CmbGenre.SelectedIndex = 0;
        }

        private void LoadBooks()
        {
            // Include — подгружает связанные таблицы (JOIN в SQL)
            // Without Include они будут null
            _allBooks = Core.DB.Books
                .Include(b => b.Users)      // автор книги
                .Include(b => b.Reviews)    // нужны для расчёта рейтинга
                .Include(b => b.Genres)     // нужны для фильтрации по жанру
                .Where(b => !b.IsFrozen)    // только незамороженные
                .ToList();

            ApplyFilters();
        }

        private void Filter_Changed(object sender, System.EventArgs e)
            => ApplyFilters();

        private void ApplyFilters()
        {
            string search = TxtSearch?.Text?.ToLower() ?? "";

            // Читаем выбранный жанр
            int genreID = 0;
            if (CmbGenre?.SelectedValue != null)
                int.TryParse(CmbGenre.SelectedValue.ToString(), out genreID);

            // Фильтруем список в памяти — без повторных запросов к БД
            IEnumerable<Books> filtered = _allBooks.Where(b =>
                (string.IsNullOrEmpty(search) ||
                 b.Title.ToLower().Contains(search) ||
                 b.Users.DisplayName.ToLower().Contains(search)) &&
                (genreID == 0 || b.Genres.Any(g => g.GenreID == genreID))
            );

            // Сортировка
            bool byRating = (CmbSort?.SelectedIndex ?? 0) == 1;
            filtered = byRating
                ? filtered.OrderByDescending(b =>
                    b.Reviews.Any() ? b.Reviews.Average(r => (double)r.Rating) : 0)
                : filtered.OrderBy(b => b.Title);

            // Перерисовываем карточки
            BooksPanel.Children.Clear();
            foreach (var book in filtered)
            {
                double avg = book.Reviews.Any()
                    ? book.Reviews.Average(r => (double)r.Rating) : 0;
                BooksPanel.Children.Add(CreateBookCard(book, avg));
            }
        }

        private Border CreateBookCard(Books book, double avgRating)
        {
            var border = new Border
            {
                Width = 170,
                Height = 235,
                Margin = new Thickness(6),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var sp = new StackPanel { Margin = new Thickness(8) };

            // Обложка — цветной блок с иконкой книги
            var cover = new Border
            {
                Height = 90,
                Background = new SolidColorBrush(Color.FromRgb(74, 144, 217)),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 7)
            };
            cover.Child = new TextBlock
            {
                Text = "📖",
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            sp.Children.Add(cover);

            sp.Children.Add(new TextBlock
            {
                Text = book.Title,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 40,
                FontSize = 12
            });
            sp.Children.Add(new TextBlock
            {
                Text = book.Users?.DisplayName ?? "—",
                Foreground = Brushes.Gray,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"⭐ {avgRating:F1}",
                FontSize = 11,
                Margin = new Thickness(0, 3, 0, 4)
            });

            // Кнопки
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

            int bookId = book.BookID; // важно: захватываем в переменную для лямбды

            var btnRead = new Button
            {
                Content = "Читать",
                FontSize = 10,
                Padding = new Thickness(5, 2, 5, 2),
                Background = new SolidColorBrush(Color.FromRgb(74, 144, 217)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnRead.Click += (s, e) => MainWindow.Instance.Navigate(new BookPage(bookId));

            var btnAdd = new Button
            {
                Content = "В список",
                FontSize = 10,
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(3, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnAdd.Click += (s, e) => ShowAddToListDialog(bookId, book.Title);

            btnPanel.Children.Add(btnRead);
            btnPanel.Children.Add(btnAdd);
            sp.Children.Add(btnPanel);
            border.Child = sp;
            return border;
        }

        private void ShowAddToListDialog(int bookId, string bookTitle)
        {
            var win = new Window
            {
                Title = "Добавить в список",
                Width = 280,
                Height = 210,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = MainWindow.Instance
            };

            var sp = new StackPanel { Margin = new Thickness(15) };
            var combo = new ComboBox { Margin = new Thickness(0, 5, 0, 10) };
            combo.Items.Add("Читаю");
            combo.Items.Add("Прочитано");
            combo.Items.Add("В планах");
            combo.Items.Add("Заброшено");
            combo.SelectedIndex = 0;

            sp.Children.Add(new TextBlock
            {
                Text = $"Книга: {bookTitle}",
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            });
            sp.Children.Add(new TextBlock { Text = "Выберите список:" });
            sp.Children.Add(combo);

            var btn = new Button
            {
                Content = "Добавить",
                Height = 32,
                Margin = new Thickness(0, 8, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btn.Click += (s, e) =>
            {
                AddToList(bookId, combo.SelectedItem.ToString());
                win.Close();
            };

            sp.Children.Add(btn);
            win.Content = sp;
            win.ShowDialog();
        }

        private void AddToList(int bookId, string section)
        {
            // Ищем — вдруг книга уже есть в каком-то списке
            var existing = Core.DB.ReadingLists
                .FirstOrDefault(r => r.UserID == SessionManager.UserID
                                  && r.BookID == bookId);
            if (existing != null)
            {
                existing.Section = section; // просто меняем раздел
            }
            else
            {
                Core.DB.ReadingLists.Add(new ReadingLists
                {
                    UserID = SessionManager.UserID,
                    BookID = bookId,
                    Section = section
                });
            }

            Core.DB.SaveChanges();
            Core.Reset();
            MessageBox.Show($"Книга добавлена в «{section}»", "Готово");
        }
    }
}
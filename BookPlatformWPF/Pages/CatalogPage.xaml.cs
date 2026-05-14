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
        private List<Books> _allBooks = new List<Books>();
        private bool _isLoaded = false;

        public CatalogPage()
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

        private void LoadBooks()
        {
            _allBooks = Core.DB.Books
                .Include(b => b.Users)
                .Include(b => b.Reviews)
                .Include(b => b.Genres)
                .Where(b => !b.IsFrozen)
                .ToList();

            ApplyFilters();
        }

        private void Filter_Changed(object sender, System.EventArgs e)
        {
            if (!_isLoaded) return;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (BooksPanel == null) return;

            string search = TxtSearch?.Text?.ToLower() ?? "";

            int genreID = 0;
            if (CmbGenre?.SelectedValue != null)
                int.TryParse(CmbGenre.SelectedValue.ToString(), out genreID);

            IEnumerable<Books> filtered = _allBooks.Where(b =>
                (string.IsNullOrEmpty(search) ||
                 b.Title.ToLower().Contains(search) ||
                 (b.Users != null && b.Users.DisplayName.ToLower().Contains(search))) &&
                (genreID == 0 || b.Genres.Any(g => g.GenreID == genreID))
            );

            bool byRating = (CmbSort?.SelectedIndex ?? 0) == 1;
            filtered = byRating
                ? filtered.OrderByDescending(b =>
                    b.Reviews.Any() ? b.Reviews.Average(r => (double)r.Rating) : 0)
                : filtered.OrderBy(b => b.Title);

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
                Height = 250,
                Margin = new Thickness(6),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var sp = new StackPanel();

            // ── ОБЛОЖКА ──────────────────────────────────────────
            var coverBorder = new Border
            {
                Height = 110,
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                ClipToBounds = true
            };

            // Если путь к картинке задан и файл существует — показываем картинку
            if (!string.IsNullOrEmpty(book.CoverPath) &&
                System.IO.File.Exists(book.CoverPath))
            {
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new System.Uri(book.CoverPath);
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();

                    coverBorder.Background = Brushes.Black;
                    coverBorder.Child = new System.Windows.Controls.Image
                    {
                        Source = bmp,
                        Stretch = System.Windows.Media.Stretch.UniformToFill
                    };
                }
                catch
                {
                    // Если картинка не загрузилась — показываем градиент с инициалами
                    coverBorder.Background = GetCoverBrush(book.BookID);
                    coverBorder.Child = MakeCoverText(book.Title);
                }
            }
            else
            {
                // Нет картинки — градиентная обложка с инициалами
                coverBorder.Background = GetCoverBrush(book.BookID);
                coverBorder.Child = MakeCoverText(book.Title);
            }

            sp.Children.Add(coverBorder);

            // ── ТЕКСТ ──────────────────────────────────────────
            var info = new StackPanel { Margin = new Thickness(8, 6, 8, 4) };

            info.Children.Add(new TextBlock
            {
                Text = book.Title,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 36,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            });
            info.Children.Add(new TextBlock
            {
                Text = book.Users?.DisplayName ?? "—",
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });

            var ratingRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 0)
            };
            ratingRow.Children.Add(new TextBlock
            {
                Text = GetStars(avgRating),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 0)),
                FontSize = 11
            });
            ratingRow.Children.Add(new TextBlock
            {
                Text = $" {avgRating:F1}",
                Foreground = Brushes.Gray,
                FontSize = 11
            });
            info.Children.Add(ratingRow);
            sp.Children.Add(info);

            // ── КНОПКИ ─────────────────────────────────────────
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(8, 0, 8, 8)
            };

            int bookId = book.BookID;

            btnPanel.Children.Add(MakeCardButton(
                "Читать", Color.FromRgb(74, 144, 217),
                () => MainWindow.Instance.Navigate(new BookPage(bookId))));

            btnPanel.Children.Add(MakeCardButton(
                "+ Список", Color.FromRgb(39, 174, 96),
                () => ShowAddToListDialog(bookId, book.Title),
                leftMargin: 4));

            sp.Children.Add(btnPanel);
            border.Child = sp;
            return border;
        }

        // Кнопка со скруглёнными углами через Border
        private Border MakeCardButton(string label, Color bg,
                                      System.Action onClick, int leftMargin = 0)
        {
            var brd = new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(leftMargin, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            brd.Child = new TextBlock
            {
                Text = label,
                Foreground = Brushes.White,
                FontSize = 11
            };
            // Клик на Border работает через MouseLeftButtonUp
            brd.MouseLeftButtonUp += (s, e) => onClick();
            return brd;
        }

        // Уникальный градиент по ID книги (8 цветовых схем)
        private Brush GetCoverBrush(int bookId)
        {
            var colors = new[]
            {
                new[] { Color.FromRgb(74,  144, 217), Color.FromRgb(30,  90, 170) },
                new[] { Color.FromRgb(39,  174,  96), Color.FromRgb(20, 120,  60) },
                new[] { Color.FromRgb(155,  89, 182), Color.FromRgb(100, 50, 140) },
                new[] { Color.FromRgb(231,  76,  60), Color.FromRgb(170, 40,  30) },
                new[] { Color.FromRgb(230, 126,  34), Color.FromRgb(170, 80,  10) },
                new[] { Color.FromRgb( 26, 188, 156), Color.FromRgb(15, 130, 110) },
                new[] { Color.FromRgb( 52,  73,  94), Color.FromRgb(30,  50,  70) },
                new[] { Color.FromRgb(241, 196,  15), Color.FromRgb(180, 140,   5) },
            };
            var pair = colors[bookId % colors.Length];
            return new LinearGradientBrush(pair[0], pair[1], 135);
        }

        // Инициалы из первых букв первых двух слов названия
        private TextBlock MakeCoverText(string title)
        {
            string initials = "?";
            var words = (title ?? "").Trim().Split(' ');
            if (words.Length >= 2)
                initials = $"{words[0][0]}{words[1][0]}".ToUpper();
            else if (words.Length == 1 && words[0].Length >= 2)
                initials = words[0].Substring(0, 2).ToUpper();
            else if (words.Length == 1 && words[0].Length == 1)
                initials = words[0].ToUpper();

            return new TextBlock
            {
                Text = initials,
                FontSize = 38,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                                          Color.FromArgb(180, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        // Рейтинг из 10 → 5 звёзд
        private string GetStars(double rating)
        {
            int stars = (int)System.Math.Round(rating / 2.0);
            stars = System.Math.Max(0, System.Math.Min(5, stars));
            return new string('★', stars) + new string('☆', 5 - stars);
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
            var existing = Core.DB.ReadingLists
                .FirstOrDefault(r => r.UserID == SessionManager.UserID
                                  && r.BookID == bookId);
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
            Core.Reset();
            MessageBox.Show($"Книга добавлена в «{section}»", "Готово");
        }
    }
}
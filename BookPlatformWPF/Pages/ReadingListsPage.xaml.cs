using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookPlatformWPF.Helpers;
using BookPlatformWPF.Models;

namespace BookPlatformWPF.Pages
{
    public partial class ReadingListsPage : Page
    {
        private string _currentSection = "Читаю";
        private List<Book> _books = new List<Book>();

        public ReadingListsPage()
        {
            InitializeComponent();
            LoadGenres();
            LoadBooks();
        }

        private void LoadGenres()
        {
            var genres = new List<Genre> { new Genre { GenreID = 0, Name = "Все жанры" } };
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT GenreID, Name FROM Genres ORDER BY Name", conn);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        genres.Add(new Genre { GenreID = (int)r["GenreID"], Name = r["Name"].ToString() });
            }
            CmbGenre.ItemsSource = genres;
            CmbGenre.SelectedIndex = 0;
        }

        private void BtnTab_Click(object sender, RoutedEventArgs e)
        {
            _currentSection = (sender as Button).Tag.ToString();
            LoadBooks();
        }

        private void Filter_Changed(object sender, EventArgs e) => ApplyFilters();

        private void LoadBooks()
        {
            _books.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT b.BookID, b.Title, b.Description, u.DisplayName AS AuthorName,
                           ISNULL(AVG(CAST(r.Rating AS FLOAT)),0) AS AvgRating,
                           STRING_AGG(g.Name, ', ') AS Genres,
                           rl.ReadingListID
                    FROM ReadingLists rl
                    JOIN Books b ON rl.BookID = b.BookID
                    JOIN Users u ON b.AuthorID = u.UserID
                    LEFT JOIN Reviews r ON b.BookID = r.BookID
                    LEFT JOIN BookGenres bg ON b.BookID = bg.BookID
                    LEFT JOIN Genres g ON bg.GenreID = g.GenreID
                    WHERE rl.UserID = @uid AND rl.Section = @section
                    GROUP BY b.BookID, b.Title, b.Description, u.DisplayName, rl.ReadingListID";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                cmd.Parameters.AddWithValue("@section", _currentSection);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        _books.Add(new Book
                        {
                            BookID = (int)reader["BookID"],
                            Title = reader["Title"].ToString(),
                            AuthorName = reader["AuthorName"].ToString(),
                            AvgRating = (double)reader["AvgRating"],
                            Genres = reader["Genres"] == DBNull.Value ? "" : reader["Genres"].ToString()
                        });
            }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string search = TxtSearch?.Text?.ToLower() ?? "";
            var filtered = _books.Where(b =>
                string.IsNullOrEmpty(search) ||
                b.Title.ToLower().Contains(search) ||
                b.AuthorName.ToLower().Contains(search)).ToList();

            bool byRating = (CmbSort?.SelectedIndex ?? 0) == 1;
            filtered = byRating ? filtered.OrderByDescending(b => b.AvgRating).ToList()
                                : filtered.OrderBy(b => b.Title).ToList();

            BooksPanel.Children.Clear();
            foreach (var book in filtered)
                BooksPanel.Children.Add(CreateCard(book));
        }

        private Border CreateCard(Book book)
        {
            var border = new Border
            {
                Width = 170,
                Height = 240,
                Margin = new Thickness(6),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6)
            };
            var sp = new StackPanel { Margin = new Thickness(8) };
            sp.Children.Add(new TextBlock { Text = book.Title, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, MaxHeight = 40 });
            sp.Children.Add(new TextBlock { Text = book.AuthorName, Foreground = Brushes.Gray, FontSize = 11 });
            sp.Children.Add(new TextBlock { Text = $"⭐ {book.AvgRating:F1}", FontSize = 11, Margin = new Thickness(0, 3, 0, 5) });

            int bookId = book.BookID;
            var btnRead = new Button
            {
                Content = "Читать",
                Padding = new Thickness(6, 3, 6, 3),
                Background = new SolidColorBrush(Color.FromRgb(74, 144, 217)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 2, 0, 2)
            };
            btnRead.Click += (s, e) => MainWindow.Instance.Navigate(new BookPage(bookId));

            // Переместить в другой список
            var cmbMove = new ComboBox { Margin = new Thickness(0, 4, 0, 2) };
            cmbMove.Items.Add("Читаю"); cmbMove.Items.Add("Прочитано");
            cmbMove.Items.Add("В планах"); cmbMove.Items.Add("Заброшено");
            cmbMove.SelectedItem = _currentSection;
            var btnMove = new Button
            {
                Content = "Переместить",
                Padding = new Thickness(6, 3, 6, 3),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 2, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                Foreground = Brushes.White
            };
            btnMove.Click += (s, e) =>
            {
                string newSection = cmbMove.SelectedItem?.ToString();
                if (newSection == null || newSection == _currentSection) return;
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
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "UPDATE ReadingLists SET Section=@s WHERE UserID=@uid AND BookID=@bid", conn);
                cmd.Parameters.AddWithValue("@s", newSection);
                cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                cmd.Parameters.AddWithValue("@bid", bookId);
                cmd.ExecuteNonQuery();
            }
            LoadBooks();
        }
    }
}
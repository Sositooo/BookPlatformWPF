using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF.Pages
{
    public partial class BookPage : Page
    {
        private readonly int _bookId;

        public BookPage(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            LoadBook();
            LoadReviews();
            BtnFreezeBook.Visibility = SessionManager.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadBook()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT b.Title, b.Description, b.Content,
                           u.DisplayName AS Author,
                           ISNULL(AVG(CAST(r.Rating AS FLOAT)),0) AS AvgRating,
                           STRING_AGG(g.Name, ', ') AS Genres
                    FROM Books b
                    JOIN Users u ON b.AuthorID = u.UserID
                    LEFT JOIN Reviews r ON b.BookID = r.BookID
                    LEFT JOIN BookGenres bg ON b.BookID = bg.BookID
                    LEFT JOIN Genres g ON bg.GenreID = g.GenreID
                    WHERE b.BookID = @id
                    GROUP BY b.Title, b.Description, b.Content, u.DisplayName";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", _bookId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        TxtTitle.Text = reader["Title"].ToString();
                        TxtAuthor.Text = "Автор: " + reader["Author"].ToString();
                        TxtGenres.Text = "Жанры: " + (reader["Genres"] == DBNull.Value ? "—" : reader["Genres"].ToString());
                        TxtRating.Text = $"⭐ Рейтинг: {(double)reader["AvgRating"]:F1} / 10";
                        TxtDesc.Text = reader["Description"]?.ToString();
                        TxtContent.Text = reader["Content"]?.ToString();
                    }
                }
            }
        }

        private void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            ReadPanel.Visibility = ReadPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
            BtnRead.Content = ReadPanel.Visibility == Visibility.Visible
                ? "📖 Скрыть текст" : "📖 Читать книгу";
        }

        private void BtnComplainBook_Click(object sender, RoutedEventArgs e)
            => ShowComplaintDialog(bookId: _bookId, reviewId: null);

        private void ShowComplaintDialog(int? bookId, int? reviewId)
        {
            var win = new Window
            {
                Title = "Жалоба",
                Width = 360,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = MainWindow.Instance
            };
            var sp = new StackPanel { Margin = new Thickness(15) };
            sp.Children.Add(new TextBlock { Text = "Причина жалобы:", Margin = new Thickness(0, 0, 0, 5) });
            var txt = new TextBox { Height = 80, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(4) };
            sp.Children.Add(txt);
            var btn = new Button
            {
                Content = "Отправить",
                Height = 32,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Foreground = Brushes.White
            };
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text)) return;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "INSERT INTO Complaints(UserID,BookID,ReviewID,Reason) VALUES(@uid,@bid,@rid,@r)";
                    var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                    cmd.Parameters.AddWithValue("@bid", bookId.HasValue ? (object)bookId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@rid", reviewId.HasValue ? (object)reviewId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@r", txt.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Жалоба отправлена.", "Готово");
                win.Close();
            };
            sp.Children.Add(btn);
            win.Content = sp;
            win.ShowDialog();
        }

        private void BtnFreezeBook_Click(object sender, RoutedEventArgs e)
        {
            var reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Укажите причину заморозки книги:", "Заморозка", "");
            if (string.IsNullOrWhiteSpace(reason)) return;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "UPDATE Books SET IsFrozen=1, FreezeReason=@r WHERE BookID=@id", conn);
                cmd.Parameters.AddWithValue("@r", reason);
                cmd.Parameters.AddWithValue("@id", _bookId);
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Книга заморожена.", "Готово");
            MainWindow.Instance.Navigate(new CatalogPage());
        }

        private void BtnSubmitReview_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtReview.Text)) return;
            int rating = int.Parse((CmbRating.SelectedItem as ComboBoxItem).Content.ToString());
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "INSERT INTO Reviews(UserID,BookID,ReviewText,Rating) VALUES(@uid,@bid,@txt,@rat)", conn);
                cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                cmd.Parameters.AddWithValue("@bid", _bookId);
                cmd.Parameters.AddWithValue("@txt", TxtReview.Text.Trim());
                cmd.Parameters.AddWithValue("@rat", rating);
                cmd.ExecuteNonQuery();
            }
            TxtReview.Clear();
            LoadReviews();
        }

        private void LoadReviews()
        {
            ReviewsPanel.Children.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"SELECT r.ReviewID, u.DisplayName, r.ReviewText, r.Rating, r.ReviewDate
                               FROM Reviews r
                               JOIN Users u ON r.UserID = u.UserID
                               WHERE r.BookID = @id ORDER BY r.ReviewDate DESC";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", _bookId);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        int rid = (int)reader["ReviewID"];
                        ReviewsPanel.Children.Add(CreateReviewCard(
                            rid,
                            reader["DisplayName"].ToString(),
                            reader["ReviewText"].ToString(),
                            (int)reader["Rating"],
                            (DateTime)reader["ReviewDate"]
                        ));
                    }
            }
        }

        private Border CreateReviewCard(int reviewId, string author, string text, int rating, DateTime date)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 0)
            };
            var sp = new StackPanel();

            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock { Text = author, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 10, 0) });
            header.Children.Add(new TextBlock { Text = $"⭐ {rating}/10", Foreground = Brushes.Orange });
            header.Children.Add(new TextBlock { Text = date.ToString("dd.MM.yyyy"), Foreground = Brushes.Gray, Margin = new Thickness(10, 0, 0, 0) });
            sp.Children.Add(header);
            sp.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });

            // Кнопки под отзывом
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
            var btnComplain = new Button
            {
                Content = "🚩 Пожаловаться",
                FontSize = 11,
                Padding = new Thickness(6, 2, 6, 2),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Foreground = Brushes.White
            };
            int rid = reviewId;
            btnComplain.Click += (s, e) => ShowComplaintDialog(bookId: null, reviewId: rid);
            btnRow.Children.Add(btnComplain);

            if (SessionManager.IsAdmin)
            {
                var btnFreeze = new Button
                {
                    Content = "❄️ Заморозить отзыв",
                    FontSize = 11,
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(5, 0, 0, 0),
                    BorderThickness = new Thickness(0),
                    Background = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                    Foreground = Brushes.White
                };
                btnFreeze.Click += (s, e) => FreezeReview(rid, border);
                btnRow.Children.Add(btnFreeze);
            }
            sp.Children.Add(btnRow);
            border.Child = sp;
            return border;
        }

        private void FreezeReview(int reviewId, Border card)
        {
            if (MessageBox.Show("Заморозить этот отзыв?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // В БД нет IsFrozen у отзывов — удаляем отзыв как заморозку
                // Или можно просто скрыть, добавив поле. Здесь — удаляем.
                var cmd = new SqlCommand("DELETE FROM Reviews WHERE ReviewID=@id", conn);
                cmd.Parameters.AddWithValue("@id", reviewId);
                cmd.ExecuteNonQuery();
            }
            ReviewsPanel.Children.Remove(card);
        }
    }
}
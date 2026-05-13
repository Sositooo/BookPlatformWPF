using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF.Pages
{
    public partial class AuthorPage : Page
    {
        public AuthorPage()
        {
            InitializeComponent();
            LoadBooks();
        }

        private void BtnAddBook_Click(object sender, RoutedEventArgs e)
            => MainWindow.Instance.Navigate(new AddEditBookPage(null));

        private void LoadBooks()
        {
            BooksPanel.Children.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"SELECT BookID, Title, IsFrozen, FreezeReason
                               FROM Books WHERE AuthorID=@uid ORDER BY Title";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        int bookId = (int)reader["BookID"];
                        bool isFrozen = (bool)reader["IsFrozen"];
                        string title = reader["Title"].ToString();
                        string reason = reader["FreezeReason"]?.ToString();
                        BooksPanel.Children.Add(CreateRow(bookId, title, isFrozen, reason));
                    }
            }
        }

        private Border CreateRow(int bookId, string title, bool isFrozen, string freezeReason)
        {
            var border = new Border
            {
                Background = isFrozen ? new SolidColorBrush(Color.FromRgb(253, 234, 234)) : Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 4, 0, 0)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };

            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock { Text = (isFrozen ? "❄️ " : "") + title, FontWeight = FontWeights.Bold });
            if (isFrozen && !string.IsNullOrEmpty(freezeReason))
                info.Children.Add(new TextBlock
                {
                    Text = "Причина: " + freezeReason,
                    Foreground = Brushes.Red,
                    FontSize = 11
                });

            var btns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 0, 0)
            };

            var btnEdit = new Button
            {
                Content = "Редактировать",
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 5, 0)
            };
            btnEdit.Click += (s, e) => MainWindow.Instance.Navigate(new AddEditBookPage(bookId));

            btns.Children.Add(btnEdit);

            if (isFrozen)
            {
                var btnAppeal = new Button
                {
                    Content = "Оспорить заморозку",
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 5, 0),
                    Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Foreground = Brushes.White
                };
                btnAppeal.Click += (s, e) => AppealBookFreeze(bookId);
                btns.Children.Add(btnAppeal);
            }

            sp.Children.Add(info);
            sp.Children.Add(btns);
            border.Child = sp;
            return border;
        }

        private void AppealBookFreeze(int bookId)
        {
            var reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Причина оспаривания заморозки книги:", "Оспорить", "");
            if (string.IsNullOrWhiteSpace(reason)) return;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "INSERT INTO UnfreezeRequests(UserID,BookID,Reason) VALUES(NULL,@bid,@r)", conn);
                cmd.Parameters.AddWithValue("@bid", bookId);
                cmd.Parameters.AddWithValue("@r", reason);
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Заявка отправлена.", "Готово");
        }
    }
}
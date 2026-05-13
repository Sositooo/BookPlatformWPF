using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadProfile();
            LoadReviews();
        }

        private void LoadProfile()
        {
            TxtName.Text = "Имя: " + SessionManager.DisplayName;
            TxtLogin.Text = "Логин: " + SessionManager.Login;
            TxtEmail.Text = "Email: " + SessionManager.Email;
            TxtRole.Text = "Роль: " + GetRoleName(SessionManager.RoleID);

            if (SessionManager.IsFrozen)
            {
                FreezeWarning.Visibility = Visibility.Visible;
                TxtFreezeReason.Text = "Причина: " +
                    (string.IsNullOrEmpty(SessionManager.FreezeReason)
                     ? "не указана" : SessionManager.FreezeReason);
            }

            // Кнопку «Стать автором» показываем только Читателям без активной заявки
            if (SessionManager.RoleID == 1 && !HasActiveRoleRequest())
                BtnApplyAuthor.Visibility = Visibility.Visible;
        }

        private string GetRoleName(int roleId) => roleId switch
        {
            1 => "Читатель",
            2 => "Автор",
            3 => "Администратор",
            _ => "—"
        };

        private bool HasActiveRoleRequest()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM RoleRequests WHERE UserID=@uid", conn);
                cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private void BtnApplyAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Подать заявку на роль Автора?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "INSERT INTO RoleRequests(UserID) VALUES(@uid)", conn);
                cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Заявка отправлена! Ожидайте решения администратора.", "Готово");
            BtnApplyAuthor.Visibility = Visibility.Collapsed;
        }

        private void BtnAppealFreeze_Click(object sender, RoutedEventArgs e)
        {
            var reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Укажите причину для оспаривания заморозки:", "Оспорить заморозку", "");
            if (string.IsNullOrWhiteSpace(reason)) return;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "INSERT INTO UnfreezeRequests(UserID,BookID,Reason) VALUES(@uid,NULL,@r)", conn);
                cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                cmd.Parameters.AddWithValue("@r", reason);
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Заявка на разморозку отправлена.", "Готово");
        }

        private void LoadReviews()
        {
            ReviewsPanel.Children.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"SELECT r.ReviewID, b.Title, r.ReviewText, r.Rating, r.ReviewDate
                               FROM Reviews r JOIN Books b ON r.BookID = b.BookID
                               WHERE r.UserID = @uid ORDER BY r.ReviewDate DESC";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@uid", SessionManager.UserID);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        var border = new Border
                        {
                            Background = Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(10),
                            Margin = new Thickness(0, 4, 0, 0)
                        };
                        var sp = new StackPanel();
                        sp.Children.Add(new TextBlock
                        {
                            Text = reader["Title"].ToString(),
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 3)
                        });
                        sp.Children.Add(new TextBlock
                        {
                            Text = $"⭐ {reader["Rating"]}/10  •  {((DateTime)reader["ReviewDate"]):dd.MM.yyyy}",
                            Foreground = Brushes.Gray,
                            FontSize = 12
                        });
                        sp.Children.Add(new TextBlock
                        {
                            Text = reader["ReviewText"].ToString(),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 4, 0, 0)
                        });
                        border.Child = sp;
                        ReviewsPanel.Children.Add(border);
                    }
            }
        }
    }
}
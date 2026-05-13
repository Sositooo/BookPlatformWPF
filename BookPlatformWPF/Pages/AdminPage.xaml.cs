using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF.Pages
{
    public partial class AdminPage : Page
    {
        public AdminPage()
        {
            InitializeComponent();
            LoadComplaints();
            LoadUnfreezeRequests();
            LoadRoleRequests();
            LoadFrozen();
            LoadUsers();
        }

        // === ЖАЛОБЫ ===
        private void BtnRefreshComplaints_Click(object sender, RoutedEventArgs e) => LoadComplaints();

        private void LoadComplaints()
        {
            ComplaintsPanel.Children.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT c.ComplaintID, c.Reason, c.ComplaintDate,
                           uc.DisplayName AS From_User,
                           b.Title AS BookTitle, r.ReviewText
                    FROM Complaints c
                    JOIN Users uc ON c.UserID = uc.UserID
                    LEFT JOIN Books b ON c.BookID = b.BookID
                    LEFT JOIN Reviews r ON c.ReviewID = r.ReviewID
                    ORDER BY c.ComplaintDate DESC";
                var cmd = new SqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        int cid = (int)reader["ComplaintID"];
                        string target = reader["BookTitle"] != DBNull.Value
                            ? "📚 Книга: " + reader["BookTitle"]
                            : "💬 Отзыв: " + reader["ReviewText"]?.ToString()?[..Math.Min(50, reader["ReviewText"].ToString().Length)] + "...";
                        ComplaintsPanel.Children.Add(
                            CreateAdminRow(cid, $"от {reader["From_User"]}  |  {target}\n{reader["Reason"]}",
                                () => DeleteComplaint(cid)));
                    }
            }
        }

        private void DeleteComplaint(int id)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                new SqlCommand($"DELETE FROM Complaints WHERE ComplaintID={id}", conn).ExecuteNonQuery();
            }
            LoadComplaints();
        }

        // === ЗАЯВКИ НА РАЗМОРОЗКУ ===
        private void BtnRefreshUnfreeze_Click(object sender, RoutedEventArgs e) => LoadUnfreezeRequests();

        private void LoadUnfreezeRequests()
        {
            UnfreezePanel.Children.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT ur.UnfreezeRequestID, ur.Reason, ur.RequestDate,
                           u.DisplayName AS UserName, u.UserID,
                           b.Title AS BookTitle, b.BookID
                    FROM UnfreezeRequests ur
                    LEFT JOIN Users u ON ur.UserID = u.UserID
                    LEFT JOIN Books b ON ur.BookID = b.BookID
                    ORDER BY ur.RequestDate DESC";
                var cmd = new SqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        int rid = (int)reader["UnfreezeRequestID"];
                        int? uid = reader["UserID"] == DBNull.Value ? null : (int?)reader["UserID"];
                        int? bid = reader["BookID"] == DBNull.Value ? null : (int?)reader["BookID"];
                        string who = uid.HasValue
                            ? "👤 Пользователь: " + reader["UserName"]
                            : "📚 Книга: " + reader["BookTitle"];
                        UnfreezePanel.Children.Add(CreateAdminRowWithApprove(
                            rid, $"{who}\n{reader["Reason"]}",
                            () => ApproveUnfreeze(rid, uid, bid),
                            () => DeleteUnfreezeRequest(rid)));
                    }
            }
        }

        private void ApproveUnfreeze(int requestId, int? userId, int? bookId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                if (userId.HasValue)
                    new SqlCommand($"UPDATE Users SET IsFrozen=0, FreezeReason=NULL WHERE UserID={userId}", conn).ExecuteNonQuery();
                else if (bookId.HasValue)
                    new SqlCommand($"UPDATE Books SET IsFrozen=0, FreezeReason=NULL WHERE BookID={bookId}", conn).ExecuteNonQuery();
                new SqlCommand($"DELETE FROM UnfreezeRequests WHERE UnfreezeRequestID={requestId}", conn).ExecuteNonQuery();
            }
            MessageBox.Show("Заморозка снята.", "Готово");
            LoadUnfreezeRequests();
        }

        private void DeleteUnfreezeRequest(int id)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                new SqlCommand($"DELETE FROM UnfreezeRequests WHERE UnfreezeRequestID={id}", conn).ExecuteNonQuery();
            }
            LoadUnfreezeRequests();
        }

        // === ЗАЯВКИ НА РОЛЬ АВТОРА ===
        private void BtnRefreshRoles_Click(object sender, RoutedEventArgs e) => LoadRoleRequests();

        private void LoadRoleRequests()
        {
            RoleRequestsPanel.Children.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"SELECT rr.RequestID, rr.RequestDate, u.DisplayName, u.UserID
                               FROM RoleRequests rr JOIN Users u ON rr.UserID = u.UserID
                               ORDER BY rr.RequestDate";
                var cmd = new SqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        int reqId = (int)reader["RequestID"];
                        int uid = (int)reader["UserID"];
                        RoleRequestsPanel.Children.Add(CreateAdminRowWithApprove(
                            reqId,
                            $"👤 {reader["DisplayName"]}  ({((DateTime)reader["RequestDate"]):dd.MM.yyyy})",
                            () => ApproveRoleRequest(reqId, uid),
                            () => DeleteRoleRequest(reqId)));
                    }
            }
        }

        private void ApproveRoleRequest(int requestId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                new SqlCommand($"UPDATE Users SET RoleID=2 WHERE UserID={userId}", conn).ExecuteNonQuery();
                new SqlCommand($"DELETE FROM RoleRequests WHERE RequestID={requestId}", conn).ExecuteNonQuery();
            }
            MessageBox.Show("Пользователю назначена роль Автора.", "Готово");
            LoadRoleRequests();
        }

        private void DeleteRoleRequest(int id)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                new SqlCommand($"DELETE FROM RoleRequests WHERE RequestID={id}", conn).ExecuteNonQuery();
            }
            LoadRoleRequests();
        }

        // === ЗАМОРОЖЕННЫЕ ===
        private void LoadFrozen()
        {
            FrozenPanel.Children.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Замороженные книги
                FrozenPanel.Children.Add(new TextBlock
                {
                    Text = "📚 Замороженные книги",
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    Margin = new Thickness(0, 0, 0, 5)
                });
                var cmd = new SqlCommand("SELECT BookID, Title, FreezeReason FROM Books WHERE IsFrozen=1", conn);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        int bid = (int)r["BookID"];
                        FrozenPanel.Children.Add(new TextBlock
                        {
                            Text = $"• {r["Title"]}  —  {r["FreezeReason"]}",
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 2, 0, 2)
                        });
                    }

                // Замороженные пользователи
                FrozenPanel.Children.Add(new TextBlock
                {
                    Text = "👤 Замороженные пользователи",
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    Margin = new Thickness(0, 10, 0, 5)
                });
                var cmd2 = new SqlCommand("SELECT UserID, DisplayName, Login, FreezeReason FROM Users WHERE IsFrozen=1", conn);
                using (var r = cmd2.ExecuteReader())
                    while (r.Read())
                        FrozenPanel.Children.Add(new TextBlock
                        {
                            Text = $"• {r["DisplayName"]} ({r["Login"]})  —  {r["FreezeReason"]}",
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 2, 0, 2)
                        });
            }
        }

        // === ПОЛЬЗОВАТЕЛИ ===
        private void BtnRefreshUsers_Click(object sender, RoutedEventArgs e) => LoadUsers();

        private void LoadUsers()
        {
            UsersPanel.Children.Clear();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"SELECT u.UserID, u.Login, u.DisplayName, u.IsFrozen,
                                      r.RoleName, u.RoleID
                               FROM Users u JOIN Roles r ON u.RoleID = r.RoleID
                               ORDER BY u.DisplayName";
                var cmd = new SqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        int uid = (int)reader["UserID"];
                        bool frozen = (bool)reader["IsFrozen"];
                        int roleId = (int)reader["RoleID"];
                        string name = reader["DisplayName"].ToString();
                        string login = reader["Login"].ToString();
                        string role = reader["RoleName"].ToString();
                        UsersPanel.Children.Add(CreateUserRow(uid, name, login, role, roleId, frozen));
                    }
            }
        }

        private Border CreateUserRow(int uid, string name, string login, string role, int roleId, bool frozen)
        {
            var border = new Border
            {
                Background = frozen ? new SolidColorBrush(Color.FromRgb(253, 234, 234)) : Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 3, 0, 0)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, MinWidth = 250 };
            info.Children.Add(new TextBlock { Text = $"{name} ({login})", FontWeight = FontWeights.Bold });
            info.Children.Add(new TextBlock { Text = "Роль: " + role, FontSize = 12, Foreground = Brushes.Gray });

            var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 0, 0) };

            // Смена роли
            var cmbRole = new ComboBox { Width = 120 };
            cmbRole.Items.Add(new ComboBoxItem { Content = "Читатель", Tag = 1 });
            cmbRole.Items.Add(new ComboBoxItem { Content = "Автор", Tag = 2 });
            cmbRole.Items.Add(new ComboBoxItem { Content = "Администратор", Tag = 3 });
            cmbRole.SelectedIndex = roleId - 1;
            var btnRole = new Button { Content = "Сменить роль", Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(4, 0, 0, 0) };
            btnRole.Click += (s, e) =>
            {
                int newRole = (int)((ComboBoxItem)cmbRole.SelectedItem).Tag;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    new SqlCommand($"UPDATE Users SET RoleID={newRole} WHERE UserID={uid}", conn).ExecuteNonQuery();
                }
                LoadUsers();
            };

            // Сменить пароль
            var btnPwd = new Button { Content = "Сменить пароль", Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(4, 0, 0, 0) };
            btnPwd.Click += (s, e) =>
            {
                string newPwd = Microsoft.VisualBasic.Interaction.InputBox("Новый пароль:", "Смена пароля", "");
                if (string.IsNullOrWhiteSpace(newPwd)) return;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("UPDATE Users SET Password=@p WHERE UserID=@uid", conn);
                    cmd.Parameters.AddWithValue("@p", newPwd);
                    cmd.Parameters.AddWithValue("@uid", uid);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Пароль изменён.", "Готово");
            };

            // Заморозить/разморозить
            var btnFreeze = new Button
            {
                Content = frozen ? "✅ Разморозить" : "❄️ Заморозить",
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(4, 0, 0, 0),
                Background = frozen ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                                    : new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnFreeze.Click += (s, e) =>
            {
                if (frozen)
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        new SqlCommand($"UPDATE Users SET IsFrozen=0,FreezeReason=NULL WHERE UserID={uid}", conn).ExecuteNonQuery();
                    }
                }
                else
                {
                    string r = Microsoft.VisualBasic.Interaction.InputBox("Причина заморозки:", "Заморозка", "");
                    if (string.IsNullOrWhiteSpace(r)) return;
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand("UPDATE Users SET IsFrozen=1,FreezeReason=@r WHERE UserID=@uid", conn);
                        cmd.Parameters.AddWithValue("@r", r);
                        cmd.Parameters.AddWithValue("@uid", uid);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadUsers();
            };

            btns.Children.Add(cmbRole);
            btns.Children.Add(btnRole);
            btns.Children.Add(btnPwd);
            btns.Children.Add(btnFreeze);
            sp.Children.Add(info);
            sp.Children.Add(btns);
            border.Child = sp;
            return border;
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

        // Карточка с кнопкой "Отклонить" (для жалоб)
        private Border CreateAdminRow(int id, string text, Action onDecline)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 3, 0, 0)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var info = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 550
            };
            var btnDecline = new Button
            {
                Content = "✖ Отклонить",
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(10, 0, 0, 0),
                Background = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnDecline.Click += (s, e) => onDecline();
            sp.Children.Add(info);
            sp.Children.Add(btnDecline);
            border.Child = sp;
            return border;
        }

        // Карточка с кнопками "Принять" и "Отклонить"
        private Border CreateAdminRowWithApprove(int id, string text, Action onApprove, Action onDecline)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 3, 0, 0)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var info = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 500
            };

            var btnApprove = new Button
            {
                Content = "✔ Принять",
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(10, 0, 4, 0),
                Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            btnApprove.Click += (s, e) => onApprove();

            var btnDecline = new Button
            {
                Content = "✖ Отклонить",
                Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            btnDecline.Click += (s, e) => onDecline();

            sp.Children.Add(info);
            sp.Children.Add(btnApprove);
            sp.Children.Add(btnDecline);
            border.Child = sp;
            return border;
        }
    }
}
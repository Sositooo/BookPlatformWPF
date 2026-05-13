using System;
using System.Data.Entity;
using System.Linq;
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

        // ══════════════════════════════════════════════════════
        // ЖАЛОБЫ
        // ══════════════════════════════════════════════════════
        private void BtnRefreshComplaints_Click(object sender, RoutedEventArgs e)
            => LoadComplaints();

        private void LoadComplaints()
        {
            ComplaintsPanel.Children.Clear();

            var list = Core.DB.Complaints
                .Include(c => c.Users)   // кто пожаловался
                .Include(c => c.Books)   // на какую книгу
                .Include(c => c.Reviews) // или на какой отзыв
                .OrderByDescending(c => c.ComplaintDate)
                .ToList();

            if (!list.Any())
            {
                ComplaintsPanel.Children.Add(NoData("Жалоб нет."));
                return;
            }

            foreach (var c in list)
            {
                // Формируем текст: на что жалоба
                string target = c.Books != null
                    ? "📚 Книга: " + c.Books.Title
                    : "💬 Отзыв: " + Truncate(c.Reviews?.ReviewText, 60);

                string info = $"От: {c.Users?.DisplayName ?? "—"}\n{target}\nПричина: {c.Reason}";
                int cid = c.ComplaintID;

                // У жалобы только кнопка "Отклонить" (закрыть жалобу)
                ComplaintsPanel.Children.Add(
                    MakeRow(info, onDecline: () => DeleteComplaint(cid)));
            }
        }

        private void DeleteComplaint(int id)
        {
            var c = Core.DB.Complaints.Find(id);
            if (c == null) return;
            Core.DB.Complaints.Remove(c);
            Core.DB.SaveChanges();
            Core.Reset();
            LoadComplaints();
        }

        // ══════════════════════════════════════════════════════
        // ЗАЯВКИ НА РАЗМОРОЗКУ
        // ══════════════════════════════════════════════════════
        private void BtnRefreshUnfreeze_Click(object sender, RoutedEventArgs e)
            => LoadUnfreezeRequests();

        private void LoadUnfreezeRequests()
        {
            UnfreezePanel.Children.Clear();

            var list = Core.DB.UnfreezeRequests
                .Include(r => r.Users)
                .Include(r => r.Books)
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            if (!list.Any())
            {
                UnfreezePanel.Children.Add(NoData("Заявок на разморозку нет."));
                return;
            }

            foreach (var req in list)
            {
                string who = req.Users != null
                    ? "👤 Пользователь: " + req.Users.DisplayName
                    : "📚 Книга: " + (req.Books?.Title ?? "—");

                string info = $"{who}\nПричина: {req.Reason}";
                int rid = req.UnfreezeRequestID;
                int? uid = req.UserID;
                int? bid = req.BookID;

                UnfreezePanel.Children.Add(
                    MakeRowWithApprove(
                        info,
                        onApprove: () => ApproveUnfreeze(rid, uid, bid),
                        onDecline: () => DeleteUnfreezeReq(rid)
                    ));
            }
        }

        private void ApproveUnfreeze(int reqId, int? userId, int? bookId)
        {
            if (userId.HasValue)
            {
                var user = Core.DB.Users.Find(userId.Value);
                if (user != null) { user.IsFrozen = false; user.FreezeReason = null; }
            }
            else if (bookId.HasValue)
            {
                var book = Core.DB.Books.Find(bookId.Value);
                if (book != null) { book.IsFrozen = false; book.FreezeReason = null; }
            }

            var req = Core.DB.UnfreezeRequests.Find(reqId);
            if (req != null) Core.DB.UnfreezeRequests.Remove(req);

            Core.DB.SaveChanges();
            Core.Reset();

            MessageBox.Show("Заморозка снята.", "Готово");
            LoadUnfreezeRequests();
            LoadFrozen(); // обновляем список замороженных
        }

        private void DeleteUnfreezeReq(int id)
        {
            var req = Core.DB.UnfreezeRequests.Find(id);
            if (req == null) return;
            Core.DB.UnfreezeRequests.Remove(req);
            Core.DB.SaveChanges();
            Core.Reset();
            LoadUnfreezeRequests();
        }

        // ══════════════════════════════════════════════════════
        // ЗАЯВКИ НА РОЛЬ АВТОРА
        // ══════════════════════════════════════════════════════
        private void BtnRefreshRoles_Click(object sender, RoutedEventArgs e)
            => LoadRoleRequests();

        private void LoadRoleRequests()
        {
            RoleRequestsPanel.Children.Clear();

            var list = Core.DB.RoleRequests
                .Include(r => r.Users)
                .OrderBy(r => r.RequestDate)
                .ToList();

            if (!list.Any())
            {
                RoleRequestsPanel.Children.Add(NoData("Заявок нет."));
                return;
            }

            foreach (var req in list)
            {
                string info = $"👤 {req.Users?.DisplayName ?? "—"}  " +
                              $"({req.RequestDate:dd.MM.yyyy})";
                int reqId = req.RequestID;
                int uid = req.UserID;

                RoleRequestsPanel.Children.Add(
                    MakeRowWithApprove(
                        info,
                        onApprove: () => ApproveRole(reqId, uid),
                        onDecline: () => DeleteRoleReq(reqId)
                    ));
            }
        }

        private void ApproveRole(int reqId, int userId)
        {
            // Повышаем пользователя до Автора
            var user = Core.DB.Users.Find(userId);
            if (user != null) user.RoleID = 2;

            var req = Core.DB.RoleRequests.Find(reqId);
            if (req != null) Core.DB.RoleRequests.Remove(req);

            Core.DB.SaveChanges();
            Core.Reset();

            MessageBox.Show("Роль Автора назначена.", "Готово");
            LoadRoleRequests();
        }

        private void DeleteRoleReq(int id)
        {
            var req = Core.DB.RoleRequests.Find(id);
            if (req == null) return;
            Core.DB.RoleRequests.Remove(req);
            Core.DB.SaveChanges();
            Core.Reset();
            LoadRoleRequests();
        }

        // ══════════════════════════════════════════════════════
        // ЗАМОРОЖЕННЫЕ — просмотр списка
        // ══════════════════════════════════════════════════════
        private void LoadFrozen()
        {
            FrozenPanel.Children.Clear();

            // Замороженные книги
            FrozenPanel.Children.Add(SectionHeader("📚 Замороженные книги"));
            var books = Core.DB.Books.Where(b => b.IsFrozen).OrderBy(b => b.Title).ToList();
            if (!books.Any())
                FrozenPanel.Children.Add(NoData("Нет замороженных книг."));
            else
                foreach (var b in books)
                    FrozenPanel.Children.Add(new TextBlock
                    {
                        Text = $"• {b.Title}  —  {b.FreezeReason ?? "без причины"}",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2)
                    });

            // Замороженные пользователи
            FrozenPanel.Children.Add(SectionHeader("👤 Замороженные пользователи"));
            var users = Core.DB.Users.Where(u => u.IsFrozen).OrderBy(u => u.DisplayName).ToList();
            if (!users.Any())
                FrozenPanel.Children.Add(NoData("Нет замороженных пользователей."));
            else
                foreach (var u in users)
                    FrozenPanel.Children.Add(new TextBlock
                    {
                        Text = $"• {u.DisplayName} ({u.Login})  —  {u.FreezeReason ?? "без причины"}",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2)
                    });
        }

        // ══════════════════════════════════════════════════════
        // ПОЛЬЗОВАТЕЛИ
        // ══════════════════════════════════════════════════════
        private void BtnRefreshUsers_Click(object sender, RoutedEventArgs e)
            => LoadUsers();

        private void LoadUsers()
        {
            UsersPanel.Children.Clear();

            var users = Core.DB.Users
                .Include(u => u.Roles) // нужен RoleName
                .OrderBy(u => u.DisplayName)
                .ToList();

            foreach (var user in users)
                UsersPanel.Children.Add(CreateUserRow(user));
        }

        private Border CreateUserRow(Users user)
        {
            var border = new Border
            {
                Background = user.IsFrozen
                    ? new SolidColorBrush(Color.FromRgb(253, 234, 234))
                    : Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 3, 0, 0)
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var info = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 220
            };
            info.Children.Add(new TextBlock
            {
                Text = $"{user.DisplayName} ({user.Login})",
                FontWeight = FontWeights.Bold
            });
            info.Children.Add(new TextBlock
            {
                Text = "Роль: " + (user.Roles?.RoleName ?? "—"),
                Foreground = Brushes.Gray,
                FontSize = 12
            });

            var btns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };

            // Смена роли
            var cmbRole = new ComboBox { Width = 135 };
            cmbRole.Items.Add(new ComboBoxItem { Content = "Читатель", Tag = 1 });
            cmbRole.Items.Add(new ComboBoxItem { Content = "Автор", Tag = 2 });
            cmbRole.Items.Add(new ComboBoxItem { Content = "Администратор", Tag = 3 });
            cmbRole.SelectedIndex = user.RoleID - 1;

            int uid = user.UserID;
            var btnRole = new Button
            {
                Content = "Сменить роль",
                Padding = new Thickness(7, 3, 7, 3),
                Margin = new Thickness(4, 0, 0, 0)
            };
            btnRole.Click += (s, e) =>
            {
                int newRole = (int)((ComboBoxItem)cmbRole.SelectedItem).Tag;
                var u = Core.DB.Users.Find(uid);
                if (u == null) return;
                u.RoleID = newRole;
                Core.DB.SaveChanges();
                Core.Reset();
                LoadUsers();
            };

            // Смена пароля
            var btnPwd = new Button
            {
                Content = "Пароль",
                Padding = new Thickness(7, 3, 7, 3),
                Margin = new Thickness(4, 0, 0, 0)
            };
            btnPwd.Click += (s, e) =>
            {
                string pwd = Microsoft.VisualBasic.Interaction.InputBox(
                    "Новый пароль:", "Смена пароля", "");
                if (string.IsNullOrWhiteSpace(pwd)) return;
                var u = Core.DB.Users.Find(uid);
                if (u == null) return;
                u.Password = pwd;
                Core.DB.SaveChanges();
                Core.Reset();
                MessageBox.Show("Пароль изменён.", "Готово");
            };

            // Заморозить / Разморозить
            bool frozen = user.IsFrozen;
            var btnFreeze = new Button
            {
                Content = frozen ? "✅ Разморозить" : "❄️ Заморозить",
                Padding = new Thickness(7, 3, 7, 3),
                Margin = new Thickness(4, 0, 0, 0),
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                Background = frozen
                    ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                    : new SolidColorBrush(Color.FromRgb(231, 76, 60))
            };
            btnFreeze.Click += (s, e) =>
            {
                var u = Core.DB.Users.Find(uid);
                if (u == null) return;

                if (u.IsFrozen)
                {
                    u.IsFrozen = false;
                    u.FreezeReason = null;
                    Core.DB.SaveChanges();
                    Core.Reset();
                }
                else
                {
                    string reason = Microsoft.VisualBasic.Interaction.InputBox(
                        "Причина заморозки:", "Заморозка", "");
                    if (string.IsNullOrWhiteSpace(reason)) return;
                    u.IsFrozen = true;
                    u.FreezeReason = reason;
                    Core.DB.SaveChanges();
                    Core.Reset();
                }
                LoadUsers();
                LoadFrozen(); // обновляем вкладку замороженных
            };

            btns.Children.Add(cmbRole);
            btns.Children.Add(btnRole);
            btns.Children.Add(btnPwd);
            btns.Children.Add(btnFreeze);

            row.Children.Add(info);
            row.Children.Add(btns);
            border.Child = row;
            return border;
        }

        // ══════════════════════════════════════════════════════
        // ВСПОМОГАТЕЛЬНЫЕ — строки списков
        // ══════════════════════════════════════════════════════

        // Строка только с кнопкой "Отклонить" (жалобы)
        private Border MakeRow(string text, Action onDecline)
        {
            var border = MakeBorder();
            var sp = new StackPanel { Orientation = Orientation.Horizontal };

            sp.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 560,
                VerticalAlignment = VerticalAlignment.Center
            });

            var btn = new Button
            {
                Content = "✖ Отклонить",
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Click += (s, e) => onDecline();
            sp.Children.Add(btn);

            border.Child = sp;
            return border;
        }

        // Строка с кнопками "Принять" и "Отклонить" (заявки)
        private Border MakeRowWithApprove(string text, Action onApprove, Action onDecline)
        {
            var border = MakeBorder();
            var sp = new StackPanel { Orientation = Orientation.Horizontal };

            sp.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500,
                VerticalAlignment = VerticalAlignment.Center
            });

            var btnOk = new Button
            {
                Content = "✔ Принять",
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(10, 0, 4, 0),
                Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            btnOk.Click += (s, e) => onApprove();

            var btnNo = new Button
            {
                Content = "✖ Отклонить",
                Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            btnNo.Click += (s, e) => onDecline();

            sp.Children.Add(btnOk);
            sp.Children.Add(btnNo);
            border.Child = sp;
            return border;
        }

        private Border MakeBorder() => new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 3, 0, 0)
        };

        private TextBlock SectionHeader(string text) => new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.Bold,
            FontSize = 15,
            Margin = new Thickness(0, 10, 0, 5)
        };

        private TextBlock NoData(string text) => new TextBlock
        {
            Text = text,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 3)
        };

        // Обрезаем длинный текст для превью
        private string Truncate(string s, int max) =>
            s == null ? "—" : s.Length > max ? s.Substring(0, max) + "…" : s;
    }
}
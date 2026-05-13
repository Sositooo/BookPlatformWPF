using System.Data.Entity;
using System.Linq;
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
            // Данные берём из SessionManager — там хранится EF-объект пользователя
            TxtName.Text = "Имя: " + SessionManager.DisplayName;
            TxtLogin.Text = "Логин: " + SessionManager.Login;
            TxtEmail.Text = "Email: " + SessionManager.Email;
            TxtRole.Text = "Роль: " + RoleName(SessionManager.RoleID);

            // Показываем предупреждение если аккаунт заморожен
            if (SessionManager.IsFrozen)
            {
                FreezeWarning.Visibility = Visibility.Visible;
                TxtFreezeReason.Text = "Причина: " +
                    (string.IsNullOrEmpty(SessionManager.FreezeReason)
                        ? "не указана"
                        : SessionManager.FreezeReason);
            }

            // Кнопку заявки показываем только Читателям без активной заявки
            if (SessionManager.RoleID == 1)
            {
                bool hasRequest = Core.DB.RoleRequests
                    .Any(r => r.UserID == SessionManager.UserID);
                if (!hasRequest)
                    BtnApplyAuthor.Visibility = Visibility.Visible;
            }
        }

        private string RoleName(int id)
        {
            switch (id)
            {
                case 1: return "Читатель";
                case 2: return "Автор";
                case 3: return "Администратор";
                default: return "—";
            }
        }

        // Подать заявку на роль Автора
        private void BtnApplyAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Подать заявку на роль Автора?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            Core.DB.RoleRequests.Add(new RoleRequests
            {
                UserID = SessionManager.UserID
                // RequestDate ставится через DEFAULT в БД
            });

            Core.DB.SaveChanges();
            Core.Reset();

            MessageBox.Show("Заявка отправлена! Ожидайте решения.", "Готово");
            BtnApplyAuthor.Visibility = Visibility.Collapsed;
        }

        // Оспорить заморозку аккаунта
        private void BtnAppealFreeze_Click(object sender, RoutedEventArgs e)
        {
            // InputBox — из Microsoft.VisualBasic, которую добавили в References
            string reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Укажите причину для оспаривания:", "Оспорить заморозку", "");
            if (string.IsNullOrWhiteSpace(reason)) return;

            Core.DB.UnfreezeRequests.Add(new UnfreezeRequests
            {
                UserID = SessionManager.UserID,
                BookID = null,   // это заявка на пользователя, не на книгу
                Reason = reason
            });

            Core.DB.SaveChanges();
            Core.Reset();

            MessageBox.Show("Заявка на разморозку отправлена.", "Готово");
        }

        // Загрузить все отзывы пользователя
        private void LoadReviews()
        {
            ReviewsPanel.Children.Clear();

            var reviews = Core.DB.Reviews
                .Include(r => r.Books) // нужен Title книги
                .Where(r => r.UserID == SessionManager.UserID)
                .OrderByDescending(r => r.ReviewDate)
                .ToList();

            if (!reviews.Any())
            {
                ReviewsPanel.Children.Add(new TextBlock
                {
                    Text = "Вы ещё не оставляли отзывов.",
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 5)
                });
                return;
            }

            foreach (var review in reviews)
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
                    Text = review.Books?.Title ?? "—",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 3)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = $"⭐ {review.Rating}/10  •  {review.ReviewDate:dd.MM.yyyy}",
                    Foreground = Brushes.Gray,
                    FontSize = 12
                });
                sp.Children.Add(new TextBlock
                {
                    Text = review.ReviewText,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                border.Child = sp;
                ReviewsPanel.Children.Add(border);
            }
        }
    }
}
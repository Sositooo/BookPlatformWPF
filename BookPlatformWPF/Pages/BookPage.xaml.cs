using System.Data.Entity;
using System.Linq;
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
            // Кнопка заморозки — только для администратора
            BtnFreezeBook.Visibility = SessionManager.IsAdmin
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadBook()
        {
            // Загружаем книгу со всеми связями
            var book = Core.DB.Books
                .Include(b => b.Users)    // автор
                .Include(b => b.Reviews)  // для рейтинга
                .Include(b => b.Genres)   // жанры
                .FirstOrDefault(b => b.BookID == _bookId);

            if (book == null) return;

            TxtTitle.Text = book.Title;
            TxtAuthor.Text = "Автор: " + (book.Users?.DisplayName ?? "—");
            TxtGenres.Text = "Жанры: " +
                (book.Genres.Any()
                    ? string.Join(", ", book.Genres.Select(g => g.Name))
                    : "не указаны");

            double avg = book.Reviews.Any()
                ? book.Reviews.Average(r => (double)r.Rating) : 0;
            TxtRating.Text = $"⭐ Рейтинг: {avg:F1} / 10";
            TxtDesc.Text = book.Description;
            TxtContent.Text = book.Content;
        }

        // Показать / скрыть текст книги
        private void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            bool visible = ReadPanel.Visibility == Visibility.Visible;
            ReadPanel.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            BtnRead.Content = visible ? "📖 Читать книгу" : "📖 Скрыть текст";
        }

        // Жалоба на книгу
        private void BtnComplainBook_Click(object sender, RoutedEventArgs e)
            => ShowComplaintDialog(bookId: _bookId, reviewId: null);

        // Заморозить книгу (только администратор)
        private void BtnFreezeBook_Click(object sender, RoutedEventArgs e)
        {
            // InputBox из Microsoft.VisualBasic — простейший способ получить строку
            string reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Укажите причину заморозки книги:", "Заморозка", "");
            if (string.IsNullOrWhiteSpace(reason)) return;

            // Find() ищет по первичному ключу — быстро и коротко
            var book = Core.DB.Books.Find(_bookId);
            if (book == null) return;

            book.IsFrozen = true;
            book.FreezeReason = reason;
            Core.DB.SaveChanges();
            Core.Reset();

            MessageBox.Show("Книга заморожена.", "Готово");
            MainWindow.Instance.Navigate(new CatalogPage());
        }

        // Отправить отзыв
        private void BtnSubmitReview_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtReview.Text)) return;

            int rating = int.Parse(
                (CmbRating.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "5");

            Core.DB.Reviews.Add(new Reviews
            {
                UserID = SessionManager.UserID,
                BookID = _bookId,
                ReviewText = TxtReview.Text.Trim(),
                Rating = rating,
                ReviewDate = System.DateTime.Now
                
            });

            Core.DB.SaveChanges();
            Core.Reset();

            TxtReview.Clear();
            LoadReviews(); // перезагружаем список отзывов
        }

        private void LoadReviews()
        {
            ReviewsPanel.Children.Clear();

            var reviews = Core.DB.Reviews
                .Include(r => r.Users)  // нужен DisplayName автора отзыва
                .Where(r => r.BookID == _bookId)
                .OrderByDescending(r => r.ReviewDate)
                .ToList();

            foreach (var review in reviews)
                ReviewsPanel.Children.Add(CreateReviewCard(review));
        }

        private Border CreateReviewCard(Reviews review)
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

            // Шапка: имя, оценка, дата
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = review.Users?.DisplayName ?? "—",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0)
            });
            header.Children.Add(new TextBlock
            {
                Text = $"⭐ {review.Rating}/10",
                Foreground = Brushes.Orange
            });
            header.Children.Add(new TextBlock
            {
                Text = review.ReviewDate.ToString("dd.MM.yyyy"),
                Foreground = Brushes.Gray,
                Margin = new Thickness(10, 0, 0, 0)
            });

            sp.Children.Add(header);
            sp.Children.Add(new TextBlock
            {
                Text = review.ReviewText,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0)
            });

            // Кнопки под отзывом
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 7, 0, 0)
            };

            var btnComplain = new Button
            {
                Content = "🚩 Пожаловаться",
                FontSize = 11,
                Padding = new Thickness(6, 2, 6, 2),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Foreground = Brushes.White
            };
            int reviewId = review.ReviewID;
            btnComplain.Click += (s, e) =>
                ShowComplaintDialog(bookId: null, reviewId: reviewId);
            btnRow.Children.Add(btnComplain);

            // Кнопка заморозки отзыва — только администратору
            if (SessionManager.IsAdmin)
            {
                var btnFreeze = new Button
                {
                    Content = "❄️ Заморозить отзыв",
                    FontSize = 11,
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(6, 0, 0, 0),
                    BorderThickness = new Thickness(0),
                    Background = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                    Foreground = Brushes.White
                };
                btnFreeze.Click += (s, e) => FreezeReview(reviewId, border);
                btnRow.Children.Add(btnFreeze);
            }

            sp.Children.Add(btnRow);
            border.Child = sp;
            return border;
        }

        // Удалить отзыв из БД и убрать карточку с экрана
        private void FreezeReview(int reviewId, Border card)
        {
            if (MessageBox.Show("Заморозить этот отзыв?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            var review = Core.DB.Reviews.Find(reviewId);
            if (review == null) return;

            Core.DB.Reviews.Remove(review);
            Core.DB.SaveChanges();
            Core.Reset();

            ReviewsPanel.Children.Remove(card);
        }

        // Диалог жалобы — на книгу (reviewId=null) или на отзыв (bookId=null)
        private void ShowComplaintDialog(int? bookId, int? reviewId)
        {
            var win = new Window
            {
                Title = "Жалоба",
                Width = 360,
                Height = 230,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = MainWindow.Instance
            };

            var sp = new StackPanel { Margin = new Thickness(15) };
            var txt = new TextBox
            {
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Padding = new Thickness(4)
            };

            sp.Children.Add(new TextBlock
            {
                Text = "Причина жалобы:",
                Margin = new Thickness(0, 0, 0, 5)
            });
            sp.Children.Add(txt);

            var btn = new Button
            {
                Content = "Отправить",
                Height = 32,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text)) return;

                Core.DB.Complaints.Add(new Complaints
                {
                    UserID = SessionManager.UserID,
                    BookID = bookId,   // null если жалоба на отзыв
                    ReviewID = reviewId, // null если жалоба на книгу
                    Reason = txt.Text.Trim(),
                    ComplaintDate = System.DateTime.Now
                });

                Core.DB.SaveChanges();
                Core.Reset();

                MessageBox.Show("Жалоба отправлена.", "Готово");
                win.Close();
            };

            sp.Children.Add(btn);
            win.Content = sp;
            win.ShowDialog();
        }
    }
}
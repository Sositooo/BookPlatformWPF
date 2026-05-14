using System.Linq;
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

            // Только книги текущего автора
            var books = Core.DB.Books
                .Where(b => b.AuthorID == SessionManager.UserID)
                .OrderBy(b => b.Title)
                .ToList();

            if (!books.Any())
            {
                BooksPanel.Children.Add(new TextBlock
                {
                    Text = "У вас пока нет опубликованных книг.",
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 10, 0, 0)
                });
                return;
            }

            foreach (var book in books)
                BooksPanel.Children.Add(CreateRow(book));
        }

        private Border CreateRow(Books book)
        {
            var border = new Border
            {
                // Замороженные — красноватый фон
                Background = book.IsFrozen
                    ? new SolidColorBrush(Color.FromRgb(253, 234, 234))
                    : Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 4, 0, 0)
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var info = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 320
            };

            info.Children.Add(new TextBlock
            {
                Text = (book.IsFrozen ? "❄️ " : "") + book.Title,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            });

            if (book.IsFrozen && !string.IsNullOrEmpty(book.FreezeReason))
                info.Children.Add(new TextBlock
                {
                    Text = "Причина заморозки: " + book.FreezeReason,
                    Foreground = Brushes.Red,
                    FontSize = 11
                });

            var btns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 0, 0)
            };

            int bookId = book.BookID;

            var btnEdit = new Button
            {
                Content = "✏️ Редактировать",
                Padding = new Thickness(8, 4, 8, 4)
            };
            btnEdit.Click += (s, e) =>
                MainWindow.Instance.Navigate(new AddEditBookPage(bookId));
            btns.Children.Add(btnEdit);

            // Кнопка "Оспорить" — только для замороженных книг
            if (book.IsFrozen)
            {
                var btnAppeal = new Button
                {
                    Content = "⚖️ Оспорить заморозку",
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(6, 0, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };
                btnAppeal.Click += (s, e) => AppealBookFreeze(bookId);
                btns.Children.Add(btnAppeal);
            }

            row.Children.Add(info);
            row.Children.Add(btns);
            border.Child = row;
            return border;
        }

        private void AppealBookFreeze(int bookId)
        {
            string reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Причина оспаривания заморозки книги:", "Оспорить", "");
            if (string.IsNullOrWhiteSpace(reason)) return;

            Core.DB.UnfreezeRequests.Add(new UnfreezeRequests
            {
                UserID = null,    // заявка на книгу — UserID пустой
                BookID = bookId,
                Reason = reason,
                RequestDate = System.DateTime.Now
            });

            Core.DB.SaveChanges();
            Core.Reset();
            MessageBox.Show("Заявка отправлена.", "Готово");
        }
    }
}
using System.Windows;
using BookPlatformWPF.Helpers;
using BookPlatformWPF.Pages;

namespace BookPlatformWPF
{
    public partial class MainWindow : Window
    {
        // Статическая ссылка — любая страница может вызвать
        // MainWindow.Instance.Navigate(new CatalogPage())
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Настраиваем видимость пунктов меню по роли и состоянию
            RefreshSidebar();

            // По умолчанию открываем каталог
            Navigate(new CatalogPage());
        }

        // Метод навигации — вызывается из любой страницы проекта
        public void Navigate(System.Windows.Controls.Page page)
        {
            MainFrame.Navigate(page);
        }

        // Вызывай после смены роли или изменения заморозки
        public void RefreshSidebar()
        {
            BtnAuthorNav.Visibility = SessionManager.IsAuthor
                ? Visibility.Visible : Visibility.Collapsed;

            BtnAdminNav.Visibility = SessionManager.IsAdmin
                ? Visibility.Visible : Visibility.Collapsed;

            BtnFreezeWarn.Visibility = SessionManager.IsFrozen
                ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Обработчики кнопок сайдбара ──
        private void BtnCatalog_Click(object sender, RoutedEventArgs e)
            => Navigate(new CatalogPage());

        private void BtnLists_Click(object sender, RoutedEventArgs e)
            => Navigate(new ReadingListsPage());

        private void BtnAuthor_Click(object sender, RoutedEventArgs e)
            => Navigate(new AuthorPage());

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
            => Navigate(new AdminPage());

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
            => Navigate(new ProfilePage());

        // Кнопка заморозки ведёт в профиль где показано предупреждение
        private void BtnFreezeWarn_Click(object sender, RoutedEventArgs e)
            => Navigate(new ProfilePage());
    }
}
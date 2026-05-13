using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Navigation;
using BookPlatformWPF.Helpers;
using BookPlatformWPF.Pages;

namespace BookPlatformWPF
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Настраиваем видимость пунктов сайдбара
            BtnAuthorNav.Visibility = SessionManager.IsAuthor ? Visibility.Visible : Visibility.Collapsed;
            BtnAdminNav.Visibility = SessionManager.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnFreezeWarn.Visibility = SessionManager.IsFrozen ? Visibility.Visible : Visibility.Collapsed;

            // Открываем каталог по умолчанию
            Navigate(new CatalogPage());
        }

        public void Navigate(System.Windows.Controls.Page page)
        {
            MainFrame.Navigate(page);
        }

        public void RefreshSidebar()
        {
            BtnAuthorNav.Visibility = SessionManager.IsAuthor ? Visibility.Visible : Visibility.Collapsed;
            BtnAdminNav.Visibility = SessionManager.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnFreezeWarn.Visibility = SessionManager.IsFrozen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnCatalog_Click(object sender, RoutedEventArgs e) => Navigate(new CatalogPage());
        private void BtnLists_Click(object sender, RoutedEventArgs e) => Navigate(new ReadingListsPage());
        private void BtnAuthor_Click(object sender, RoutedEventArgs e) => Navigate(new AuthorPage());
        private void BtnAdmin_Click(object sender, RoutedEventArgs e) => Navigate(new AdminPage());
        private void BtnProfile_Click(object sender, RoutedEventArgs e) => Navigate(new ProfilePage());
        private void BtnFreezeWarn_Click(object sender, RoutedEventArgs e)
        {
            // Переходим в профиль где есть секция заморозки
            Navigate(new ProfilePage());
        }
    }
}
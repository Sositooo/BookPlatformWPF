using System.Linq;
using System.Windows;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF
{
    public partial class LoginWindow : Window
    {
        public LoginWindow() { InitializeComponent(); }

        private void TabControl_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
            => LblLoginError.Text = "";

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtLoginIn.Text.Trim();
            string password = TxtPasswordIn.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            { LblLoginError.Text = "Заполните все поля."; return; }

            // Ищем пользователя через EF
            var user = Core.DB.Users
                .FirstOrDefault(u => u.Login == login && u.Password == password);

            if (user != null)
            {
                SessionManager.CurrentUser = user;
                new MainWindow().Show();
                this.Close();
            }
            else
            {
                LblLoginError.Text = "Неверный логин или пароль.";
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtLoginReg.Text.Trim();
            string dispName = TxtDisplayName.Text.Trim();
            string email = TxtEmailReg.Text.Trim();
            string password = TxtPasswordReg.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(dispName) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            { LblRegError.Text = "Заполните все поля."; return; }

            bool exists = Core.DB.Users.Any(u => u.Login == login || u.Email == email);
            if (exists) { LblRegError.Text = "Логин или email уже занят."; return; }

            var newUser = new Users
            {
                Login = login,
                Password = password,
                Email = email,
                DisplayName = dispName,
                RoleID = 1,    // Читатель
                IsFrozen = false
            };

            Core.DB.Users.Add(newUser);
            Core.DB.SaveChanges();

            MessageBox.Show("Регистрация успешна! Войдите в аккаунт.", "Готово");
            TabControl.SelectedIndex = 0;
        }
    }
}
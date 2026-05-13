using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BookPlatformWPF.Helpers;

namespace BookPlatformWPF
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Сбрасываем текст ошибок при переключении вкладок
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LblLoginError != null) LblLoginError.Text = "";
            if (LblRegError != null) LblRegError.Text = "";
        }

        // ── ВХОД ──────────────────────────────────────────────────────────
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtLoginIn.Text.Trim();
            string password = TxtPasswordIn.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                LblLoginError.Text = "Заполните все поля.";
                return;
            }

            // FirstOrDefault вернёт null если пользователь не найден
            // EF сам сформирует SQL: SELECT * FROM Users WHERE Login=? AND Password=?
            var user = Core.DB.Users
                .FirstOrDefault(u => u.Login == login && u.Password == password);

            if (user != null)
            {
                // Сохраняем весь объект — теперь SessionManager.UserID,
                // SessionManager.RoleID и т.д. работают автоматически
                SessionManager.CurrentUser = user;

                new MainWindow().Show();
                this.Close();
            }
            else
            {
                LblLoginError.Text = "Неверный логин или пароль.";
            }
        }

        // ── РЕГИСТРАЦИЯ ───────────────────────────────────────────────────
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtLoginReg.Text.Trim();
            string dispName = TxtDisplayName.Text.Trim();
            string email = TxtEmailReg.Text.Trim();
            string password = TxtPasswordReg.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(dispName) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                LblRegError.Text = "Заполните все поля.";
                return;
            }

            // Any() — аналог COUNT(*) > 0, проверяем уникальность
            bool exists = Core.DB.Users
                .Any(u => u.Login == login || u.Email == email);

            if (exists)
            {
                LblRegError.Text = "Логин или email уже занят.";
                return;
            }

            // Создаём объект и добавляем в контекст
            var newUser = new Users
            {
                Login = login,
                Password = password,
                Email = email,
                DisplayName = dispName,
                RoleID = 1,       // 1 = Читатель
                IsFrozen = false
            };

            Core.DB.Users.Add(newUser);
            Core.DB.SaveChanges(); // аналог INSERT INTO Users ...
            Core.Reset();          // сбрасываем кеш чтобы новый юзер был виден

            MessageBox.Show("Регистрация успешна! Войдите в аккаунт.", "Готово");
            TabControl.SelectedIndex = 0;
        }
    }
}
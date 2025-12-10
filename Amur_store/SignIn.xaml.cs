using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Data.Entity; // ОБЯЗАТЕЛЬНО: для работы .Include()
using Amur_store;

namespace Amur_store
{
    public partial class SignIn : Window
    {
        private int failedLoginAttempts = 0;
        private const int maxFailedAttempts = 3;
        private string currentCaptcha = "";
        private Random captchaRandom = new Random();

        public SignIn()
        {
            InitializeComponent();
            CaptchaPanel.Visibility = Visibility.Collapsed;
        }

        private void butLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginInput = txtInput.Text.Trim();
            var password = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(loginInput) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка капчи, если она видна
            if (CaptchaPanel.Visibility == Visibility.Visible)
            {
                if (!CaptchaTextBox.Text.Equals(currentCaptcha, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Неверная капча!", "Ошибка");
                    ShowCaptcha();
                    return;
                }
            }

            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // 1. Ищем пользователя (по Логину ИЛИ по Email)
                    // Используем .Include, чтобы сразу подгрузить связанные таблицы Clients и Employees
                    var user = db.Users
                        .Include(u => u.Role)
                        .Include(u => u.Clients)
                        .Include(u => u.Employees)
                        .FirstOrDefault(u => u.Login == loginInput ||
                                             u.Clients.Any(c => c.Email == loginInput) ||
                                             u.Employees.Any(emp => emp.Email == loginInput));

                    if (user == null)
                    {
                        HandleFailedLogin();
                        return;
                    }

                    // 2. Проверяем пароль
                    // Используем твой PasswordHasher
                    if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
                    {
                        HandleFailedLogin();
                        return;
                    }

                    // --- УСПЕШНЫЙ ВХОД ---
                    failedLoginAttempts = 0;
                    CaptchaPanel.Visibility = Visibility.Collapsed;

                    // 3. Открываем окно в зависимости от роли
                    if (user.RoleID == 3) // Клиент
                    {
                        // САМОЕ ВАЖНОЕ: Находим ClientID!
                        // Так как пользователь зашел в систему, у него должна быть запись в таблице Clients
                        var client = user.Clients.FirstOrDefault();

                        if (client != null)
                        {
                            string fullName = $"{client.Name} {client.Surname}";

                            // Передаем ClientID в главное окно, чтобы работал Профиль, Корзина и Заказы
                            // Убедись, что MainWindow принимает (int id, string name)
                            Amur_store.MainWindow main = new Amur_store.MainWindow(client.ClientID, fullName);
                            main.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Ошибка: Пользователь найден, но профиль клиента отсутствует.");
                        }
                    }
                    else if (user.RoleID == 1) // Админ
                    {
                        MessageBox.Show("Вход администратора (окно не настроено)");
                    }
                    else
                    {
                        MessageBox.Show("Вход сотрудника (окно не настроено)");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}");
            }
        }

        // --- ЛОГИКА КАПЧИ ---
        private void HandleFailedLogin()
        {
            failedLoginAttempts++;
            if (failedLoginAttempts >= maxFailedAttempts)
            {
                ShowCaptcha();
                MessageBox.Show("Слишком много неудачных попыток. Введите капчу.", "Безопасность");
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль.", "Ошибка");
            }
        }

        private void ShowCaptcha()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            currentCaptcha = new string(Enumerable.Repeat(chars, 5).Select(s => s[captchaRandom.Next(s.Length)]).ToArray());
            CaptchaLabel.Text = currentCaptcha;
            CaptchaTextBox.Text = "";
            CaptchaPanel.Visibility = Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (CaptchaTextBox.Text.Equals(currentCaptcha, StringComparison.OrdinalIgnoreCase))
            {
                failedLoginAttempts = 0;
                CaptchaPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show("Капча верна. Повторите вход.");
            }
            else
            {
                MessageBox.Show("Неверная капча.");
                ShowCaptcha();
            }
        }

        // Кнопки переходов
        private void butSingUp_Click(object sender, RoutedEventArgs e) { new SignUp().Show(); this.Close(); }
        private void butRePass_Click(object sender, RoutedEventArgs e) { new ResetPassword().Show(); this.Close(); }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scale = Math.Min(this.ActualWidth / 800, this.ActualHeight / 450);
            if (SignInGrid != null) SignInGrid.LayoutTransform = new ScaleTransform(scale, scale);
        }
    }
}
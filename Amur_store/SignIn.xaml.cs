using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Amur_store
{
    /// <summary>
    /// Логика взаимодействия для SignIn.xaml
    /// </summary>
    public partial class SignIn : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        //Captcha
        private int failedLoginAttempts = 0;
        private const int maxFailedAttempts = 5;
        private string currentCaptcha = "";
        private Random captchaRandom = new Random();

        public SignIn()
        {
            InitializeComponent();

            if (!CheckDatabaseConnection(connectionString))
            {
                MessageBox.Show("Не удалось подключиться к базе данных. Проверьте настройки подключения.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            CaptchaPanel.Visibility = Visibility.Collapsed;
        }

        private bool CheckDatabaseConnection(string connectionString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ValidateEmail(string input)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(input);
                return addr.Address == input;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateCaptchaString()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 5)
              .Select(s => s[captchaRandom.Next(s.Length)]).ToArray());
        }

        private void ShowCaptcha()
        {
            currentCaptcha = GenerateCaptchaString();
            CaptchaLabel.Text = currentCaptcha;
            CaptchaTextBox.Text = "";
            CaptchaPanel.Visibility = Visibility.Visible;
        }

        private void butLogin_Click(object sender, RoutedEventArgs e)
        {
            var input = txtInput.Text.Trim();
            var password = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин/email и пароль.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool isEmail = ValidateEmail(input);

                // SQL запрос согласно структуре вашей БД
                string query = @"
                    SELECT 
                        U.UserID, 
                        U.Login, 
                        U.PasswordHash, 
                        U.RoleID,
                        R.RoleName,
                        ISNULL(C.Name + ' ' + C.Surname, 
                               ISNULL(E.Name + ' ' + E.Surname, 
                                      U.Login)) as FullName
                    FROM Users U
                    INNER JOIN Role R ON U.RoleID = R.RoleID
                    LEFT JOIN Clients C ON U.UserID = C.UserID
                    LEFT JOIN Employees E ON U.UserID = E.UserID
                    WHERE U.Login = @Input OR (C.Email = @Input) OR (E.Email = @Input)";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Input", input);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["PasswordHash"]?.ToString();
                            int roleId = Convert.ToInt32(reader["RoleID"]);
                            string roleName = reader["RoleName"]?.ToString();
                            string fullName = reader["FullName"]?.ToString();
                            int userId = Convert.ToInt32(reader["UserID"]);

                            // Проверяем, что хеш пароля существует и не пустой
                            if (string.IsNullOrEmpty(storedHash))
                            {
                                MessageBox.Show("Ошибка в данных пользователя. Обратитесь к администратору.",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Проверяем пароль с использованием PBKDF2
                            if (PasswordHasher.VerifyPassword(password, storedHash))
                            {
                                // Сброс счетчика неудачных попыток
                                failedLoginAttempts = 0;
                                CaptchaPanel.Visibility = Visibility.Collapsed;

                                // Открываем соответствующее окно в зависимости от роли
                                OpenUserWindow(roleId, userId, fullName, roleName);
                                return;
                            }
                        }

                        // Если пользователь не найден или пароль неверный
                        HandleFailedLogin();
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenUserWindow(int roleId, int userId, string fullName, string roleName)
        {
            switch (roleId)
            {
/*                case 1: // Admin
                    AdminWindow adminWindow = new AdminWindow(userId, fullName);
                    adminWindow.Show();
                    this.Hide();
                    break;*/

                /*case 2: // Employee (RoleID = 2)
                    // Сотрудник - окно управления заказами
                    EmployeeWindow empWindow = new EmployeeWindow(userId, fullName, roleName);
                    empWindow.Show();
                    this.Hide();
                    break;*/

                case 3: // Client (RoleID = 3)
                    // Клиент - открываем главное окно магазина
                    MainWindow mainWindow = new MainWindow(userId, fullName);
                    mainWindow.Show();
                    this.Hide();
                    break;



                default:
                    MessageBox.Show($"Неизвестная роль пользователя (ID: {roleId}).", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }

        private void HandleFailedLogin()
        {
            failedLoginAttempts++;

            if (failedLoginAttempts >= maxFailedAttempts)
            {
                ShowCaptcha();
                MessageBox.Show($"Слишком много неудачных попыток входа. Подтвердите, что вы не робот.",
                    "Безопасность", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Неверный логин/email или пароль.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void butSingUp_Click(object sender, RoutedEventArgs e)
        {
            SignUp window = new SignUp();
            window.Show();
            this.Hide();
        }

        private void butRePass_Click(object sender, RoutedEventArgs e)
        {
            ResetPassword window = new ResetPassword();
            window.Show();
            this.Hide();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (CaptchaPanel.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrWhiteSpace(CaptchaTextBox.Text) ||
                    !CaptchaTextBox.Text.Equals(currentCaptcha, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Неверная CAPTCHA. Попробуйте снова.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowCaptcha();
                    return;
                }

                // CAPTCHA верна, сбрасываем счетчик
                failedLoginAttempts = 0;
                CaptchaPanel.Visibility = Visibility.Collapsed;
                CaptchaTextBox.Text = "";

                MessageBox.Show("Проверка пройдена. Теперь можете попробовать войти снова.",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Адаптация размера интерфейса
            double scaleX = this.ActualWidth / 800;
            double scaleY = this.ActualHeight / 450;
            double scale = Math.Min(scaleX, scaleY);

            var grid = SignInGrid;
            if (grid != null)
            {
                foreach (var element in grid.Children)
                {
                    if (element is FrameworkElement fe)
                    {
                        fe.LayoutTransform = new ScaleTransform(scale, scale);
                    }
                }
            }
        }
    }
}
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Amur_store
{
    /// <summary>
    /// Логика взаимодействия для ResetPassword.xaml
    /// </summary>
    public partial class ResetPassword : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ResetPassword()
        {
            InitializeComponent();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scaleX = this.ActualWidth / 800;
            double scaleY = this.ActualHeight / 450;
            double scale = Math.Min(scaleX, scaleY);

            foreach (var element in ResetPasswordGrid.Children)
            {
                if (element is FrameworkElement fe)
                {
                    fe.LayoutTransform = new ScaleTransform(scale, scale);
                }
            }
        }

        private void butResetPassword_Click(object sender, RoutedEventArgs e)
        {
            string identifier = txtLogin.Text.Trim();
            string password = txtPassword.Password;
            string rePassword = txtRePassword.Password;

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(rePassword))
            {
                MessageBox.Show("Заполните все поля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != rePassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!PasswordHasher.CheckPasswordComplexity(password))
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов, " +
                              "включая заглавные и строчные буквы и цифры",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Определяем тип идентификатора
            bool isEmail = identifier.Contains("@");

            // Получаем информацию о пользователе
            UserInfo userInfo = GetUserInfoByIdentifier(identifier, isEmail);

            if (userInfo == null)
            {
                MessageBox.Show(isEmail ?
                    "Пользователь с таким email не найден" :
                    "Пользователь с таким логином не найден",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, что пользователь не администратор (опционально)
            if (userInfo.RoleID == 1) // Admin
            {
                MessageBox.Show("Восстановление пароля для администратора невозможно через эту форму. Обратитесь к системному администратору.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Обновляем пароль
            UpdatePassword(userInfo.UserID, password, userInfo.Login);
        }

        private void butExit_Click(object sender, RoutedEventArgs e)
        {
            SignIn window = new SignIn();
            window.Show();
            this.Close();
        }

        /// <summary>
        /// Информация о пользователе
        /// </summary>
        private class UserInfo
        {
            public int UserID { get; set; }
            public string Login { get; set; }
            public int RoleID { get; set; }
            public string Email { get; set; }
        }

        /// <summary>
        /// Получает информацию о пользователе по логину или email
        /// </summary>
        private UserInfo GetUserInfoByIdentifier(string identifier, bool isEmail)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query;
                if (isEmail)
                {
                    // Ищем email в таблицах Clients и Employees, получаем UserID
                    query = @"
                        SELECT 
                            U.UserID,
                            U.Login,
                            U.RoleID,
                            COALESCE(C.Email, E.Email) as Email
                        FROM Users U
                        LEFT JOIN Clients C ON U.UserID = C.UserID
                        LEFT JOIN Employees E ON U.UserID = E.UserID
                        WHERE C.Email = @Identifier OR E.Email = @Identifier";
                }
                else
                {
                    // Ищем логин в таблице Users
                    query = @"
                        SELECT 
                            U.UserID,
                            U.Login,
                            U.RoleID,
                            COALESCE(C.Email, E.Email) as Email
                        FROM Users U
                        LEFT JOIN Clients C ON U.UserID = C.UserID
                        LEFT JOIN Employees E ON U.UserID = E.UserID
                        WHERE U.Login = @Identifier";
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Identifier", identifier);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserInfo
                            {
                                UserID = Convert.ToInt32(reader["UserID"]),
                                Login = reader["Login"].ToString(),
                                RoleID = Convert.ToInt32(reader["RoleID"]),
                                Email = reader["Email"]?.ToString()
                            };
                        }
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет, существует ли пользователь с таким логином или email
        /// </summary>
        private bool CheckUserExists(string identifier, bool isEmail)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query;
                if (isEmail)
                {
                    // Проверяем email в таблицах Clients и Employees
                    query = @"
                        SELECT COUNT(*) FROM (
                            SELECT Email FROM Clients WHERE Email = @Identifier
                            UNION
                            SELECT Email FROM Employees WHERE Email = @Identifier
                        ) AS AllEmails";
                }
                else
                {
                    // Проверяем логин в таблице Users
                    query = "SELECT COUNT(*) FROM Users WHERE Login = @Identifier";
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Identifier", identifier);
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        /// <summary>
        /// Обновляет пароль пользователя
        /// </summary>
        private void UpdatePassword(int userId, string newPassword, string login)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Хешируем новый пароль с использованием PBKDF2
                string passwordHash = PasswordHasher.HashPassword(newPassword);

                // Обновляем пароль в таблице Users
                string updateQuery = "UPDATE Users SET PasswordHash = @PasswordHash WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected == 1)
                    {
                        // Получаем роль пользователя для информационного сообщения
                        string roleName = GetRoleNameByUserId(userId);

                        MessageBox.Show($"Пароль для пользователя '{login}' ({roleName}) успешно изменен! Теперь вы можете войти с новым паролем.",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Открываем окно входа
                        SignIn window = new SignIn();
                        window.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при изменении пароля. Пользователь не найден.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Получает название роли по UserID
        /// </summary>
        private string GetRoleNameByUserId(int userId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT R.RoleName 
                    FROM Users U
                    INNER JOIN Role R ON U.RoleID = R.RoleID
                    WHERE U.UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    object result = command.ExecuteScalar();

                    if (result != null)
                    {
                        return result.ToString();
                    }

                    return "Пользователь";
                }
            }
        }

        /// <summary>
        /// Получает RoleID по UserID
        /// </summary>
        private int GetRoleIdByUserId(int userId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT RoleID FROM Users WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    object result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }

                    return 0;
                }
            }
        }
    }
}
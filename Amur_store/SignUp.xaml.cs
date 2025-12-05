using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Amur_store
{
    /// <summary>
    /// Логика взаимодействия для SignUp.xaml
    /// </summary>
    public partial class SignUp : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public SignUp()
        {
            InitializeComponent();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scaleX = this.ActualWidth / 850;
            double scaleY = this.ActualHeight / 450;
            double scale = Math.Min(scaleX, scaleY);

            foreach (var element in SignUpGrid.Children)
            {
                if (element is FrameworkElement fe)
                {
                    fe.LayoutTransform = new ScaleTransform(scale, scale);
                }
            }
        }

        private bool ValidateEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void butSignUp_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;
            string rePassword = txtRePassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(rePassword))
            {
                MessageBox.Show("Заполните все поля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateEmail(email))
            {
                MessageBox.Show("Введите корректный email адрес", "Ошибка",
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

            if (password != rePassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CheckLoginExists(login))
            {
                MessageBox.Show("Такой логин уже используется", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CheckEmailExists(email))
            {
                MessageBox.Show("Такой email уже зарегистрирован", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Регистрируем как клиента (RoleID = 3)
            RegisterUser(login, email, password, 3); // 3 = Client
        }

        private void butExit_Click(object sender, RoutedEventArgs e)
        {
            SignIn window = new SignIn();
            window.Show();
            this.Close();
        }

        private bool CheckLoginExists(string login)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Users WHERE Login = @Login";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"Ошибка при проверке логина: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return true;
                }
            }
        }

        private bool CheckEmailExists(string email)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT COUNT(*) FROM (
                            SELECT Email FROM Clients WHERE Email = @Email
                            UNION
                            SELECT Email FROM Employees WHERE Email = @Email
                        ) AS AllEmails";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"Ошибка при проверке email: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return true;
                }
            }
        }

        /// <summary>
        /// Регистрирует нового пользователя с указанной ролью
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="email">Email пользователя</param>
        /// <param name="password">Пароль (в открытом виде)</param>
        /// <param name="roleId">ID роли (1=Admin, 2=Employee, 3=Client)</param>
        private void RegisterUser(string login, string email, string password, int roleId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // 1. Хешируем пароль с использованием PBKDF2
                    string passwordHash = PasswordHasher.HashPassword(password);

                    // 2. Вставляем пользователя в таблицу Users с указанной ролью
                    string insertUserQuery = @"
                        INSERT INTO Users (Login, PasswordHash, RoleID) 
                        VALUES (@Login, @PasswordHash, @RoleID);
                        SELECT SCOPE_IDENTITY();";

                    SqlCommand userCommand = new SqlCommand(insertUserQuery, connection, transaction);
                    userCommand.Parameters.AddWithValue("@Login", login);
                    userCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    userCommand.Parameters.AddWithValue("@RoleID", roleId);

                    int userId = Convert.ToInt32(userCommand.ExecuteScalar());

                    // 3. Вставляем клиента в таблицу Clients (только если роль = Client)
                    if (roleId == 3) // 3 = Client
                    {
                        string insertClientQuery = @"
                            INSERT INTO Clients (UserID, Email) 
                            VALUES (@UserID, @Email)";

                        SqlCommand clientCommand = new SqlCommand(insertClientQuery, connection, transaction);
                        clientCommand.Parameters.AddWithValue("@UserID", userId);
                        clientCommand.Parameters.AddWithValue("@Email", email);

                        clientCommand.ExecuteNonQuery();
                    }

                    // 4. Подтверждаем транзакцию
                    transaction.Commit();

                    string roleName = GetRoleNameById(roleId);
                    MessageBox.Show($"Регистрация успешно завершена! Вы зарегистрированы как {roleName}. Теперь вы можете войти в систему.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Открываем окно входа
                    SignIn window = new SignIn();
                    window.Show();
                    this.Close();
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();

                    if (ex.Number == 2627 || ex.Number == 2601)
                    {
                        if (ex.Message.Contains("Login") || ex.Message.Contains("UQ_Users_Login"))
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else if (ex.Message.Contains("Email") || ex.Message.Contains("UQ_Clients_Email"))
                        {
                            MessageBox.Show("Пользователь с таким email уже существует",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Получает название роли по ID для отображения пользователю
        /// </summary>
        private string GetRoleNameById(int roleId)
        {
            switch (roleId)
            {
                case 1:
                    return "Администратор";
                case 2:
                    return "Сотрудник";
                case 3:
                    return "Клиент";
                default:
                    return "Пользователь";
            }
        }
    }
}
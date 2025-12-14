using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Data.Entity; // Важно для .Include()

namespace Amur_store
{
    public partial class EmployeeSignIn : Window
    {
        public EmployeeSignIn()
        {
            InitializeComponent();
        }

        private void butLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string pass = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // 1. Ищем пользователя в таблице Users и сразу подгружаем данные Employees
                    var user = db.Users
                                 .Include(u => u.Employees) // Подгружаем связанного сотрудника
                                 .FirstOrDefault(u => u.Login == login);

                    if (user == null)
                    {
                        MessageBox.Show("Пользователь с таким логином не найден.", "Ошибка входа");
                        return;
                    }

                    // 2. Проверяем пароль
                    // Используем тот же PasswordHasher, что и для клиентов
                    bool isPasswordValid = PasswordHasher.VerifyPassword(pass, user.PasswordHash);

                    if (!isPasswordValid)
                    {
                        MessageBox.Show("Неверный пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 3. Проверяем Роль
                    // RoleID 1 = Admin, RoleID 2 = Employee. 
                    // RoleID 3 (Client) сюда не пускаем.
                    if (user.RoleID != 1 && user.RoleID != 2)
                    {
                        MessageBox.Show("У вас нет прав доступа к служебному модулю (Вы Клиент).", "Доступ запрещен");
                        return;
                    }

                    // 4. Получаем данные сотрудника
                    // У пользователя может быть список Employees, берем первого (связь 1 к 1 или 1 к N)
                    // 4. Получаем данные сотрудника
                    var employee = user.Employees.FirstOrDefault();

                    if (employee != null)
                    {
                        string fullName = $"{employee.Surname} {employee.Name}";

                        // --- ИСПРАВЛЕНИЕ: Передаем RoleID из пользователя (1 или 2) ---
                        MainWindow workWindow = new MainWindow(employee.EmployeeID, fullName, user.RoleID);

                        workWindow.Show();
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе: {ex.Message}\n{ex.InnerException?.Message}");
            }
        }

        private void butBack_Click(object sender, RoutedEventArgs e)
        {
            new SignIn().Show();
            this.Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
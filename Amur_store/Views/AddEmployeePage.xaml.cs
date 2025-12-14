using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity; // Для .Include

namespace Amur_store.Views
{
    public partial class AddEmployeePage : Page
    {
        public AddEmployeePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // 1. Загружаем должности в ComboBox
                    cbPosition.ItemsSource = db.Position.ToList();

                    // 2. Загружаем список сотрудников для таблицы
                    // ВАЖНО: Включить связанные данные (Users, Position)
                    var employees = db.Employees
                                      .Include(em => em.Position)
                                      .Include(em => em.Users)
                                      .ToList();

                    EmployeesGrid.ItemsSource = employees;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        // --- ДОБАВЛЕНИЕ СОТРУДНИКА ---
        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(tbSurname.Text) ||
                string.IsNullOrWhiteSpace(tbName.Text) ||
                string.IsNullOrWhiteSpace(tbLogin.Text) ||
                string.IsNullOrWhiteSpace(pbPassword.Password) ||
                cbPosition.SelectedItem == null)
            {
                MessageBox.Show("Заполните все обязательные поля (ФИО, Логин, Пароль, Должность)!", "Внимание");
                return;
            }

            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // Проверка логина на уникальность
                    if (db.Users.Any(u => u.Login == tbLogin.Text))
                    {
                        MessageBox.Show("Такой логин уже занят!", "Ошибка");
                        return;
                    }

                    // 1. Создаем пользователя (Таблица Users)
                    // RoleID = 2 (Сотрудник). Хешируем пароль.
                    var newUser = new Users
                    {
                        Login = tbLogin.Text.Trim(),
                        PasswordHash = PasswordHasher.HashPassword(pbPassword.Password),
                        RoleID = 2
                    };

                    db.Users.Add(newUser);
                    db.SaveChanges(); // Сохраняем, чтобы получить UserID

                    // 2. Создаем сотрудника (Таблица Employees)
                    var newEmp = new Employees
                    {
                        UserID = newUser.UserID, // Связь
                        Surname = tbSurname.Text.Trim(),
                        Name = tbName.Text.Trim(),
                        Patronymic = tbPatronymic.Text.Trim(),
                        Phone = tbPhone.Text.Trim(),
                        Email = tbEmail.Text.Trim(),
                        PositionID = (int)cbPosition.SelectedValue
                    };

                    db.Employees.Add(newEmp);
                    db.SaveChanges();

                    MessageBox.Show("Сотрудник успешно добавлен!", "Успех");

                    // Очистка полей и обновление таблицы
                    ClearForm();
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }

        // --- УДАЛЕНИЕ СОТРУДНИКА ---
        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            int empId = (int)btn.Tag;

            if (MessageBox.Show($"Вы точно хотите удалить сотрудника ID {empId}?\nЭто действие нельзя отменить.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new AmurStoreEntities())
                    {
                        var emp = db.Employees.Find(empId);
                        if (emp != null)
                        {
                            // ВАЖНО: Нельзя удалить сотрудника, если у него есть заказы (Orders)
                            // Проверяем наличие заказов
                            if (db.Orders.Any(o => o.EmployeeID == empId))
                            {
                                MessageBox.Show("Нельзя удалить сотрудника, у которого есть оформленные заказы.\nСначала переназначьте заказы на другого мастера.", "Ошибка целостности");
                                return;
                            }

                            // 1. Удаляем запись из Employees
                            int? linkedUserId = emp.UserID; // Запоминаем ID пользователя перед удалением
                            db.Employees.Remove(emp);
                            db.SaveChanges();

                            // 2. Если есть связанный User, удаляем и его (чистка базы)
                            if (linkedUserId != null)
                            {
                                var user = db.Users.Find(linkedUserId);
                                if (user != null)
                                {
                                    db.Users.Remove(user);
                                    db.SaveChanges();
                                }
                            }

                            MessageBox.Show("Сотрудник удален.");
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось удалить сотрудника.\nВозможно, есть связанные данные в других таблицах.\n" + ex.Message);
                }
            }
        }

        private void ClearForm()
        {
            tbSurname.Clear(); tbName.Clear(); tbPatronymic.Clear();
            tbPhone.Clear(); tbEmail.Clear();
            tbLogin.Clear(); pbPassword.Clear();
            cbPosition.SelectedItem = null;
        }
    }
}
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace Amur_store.Views
{
    public partial class EmployeeProfilePage : Page
    {
        private Employees _currentEmployee;
        private bool _isEditing = false;

        public EmployeeProfilePage(Employees emp)
        {
            InitializeComponent();
            _currentEmployee = emp;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Если данные уже есть в _currentEmployee, отобразим их сразу
                // Но лучше перезагрузить из базы, чтобы данные были свежими
                using (var db = new AmurStoreEntities())
                {
                    // Подгружаем должность и пользователя (для логина)
                    var emp = db.Employees
                                .Include(e => e.Position)
                                .Include(e => e.Users)
                                .FirstOrDefault(e => e.EmployeeID == _currentEmployee.EmployeeID);

                    if (emp != null)
                    {
                        _currentEmployee = emp; // Обновляем локальную переменную

                        txtSurname.Text = emp.Surname;
                        txtName.Text = emp.Name;
                        txtPatronymic.Text = emp.Patronymic;
                        txtPhone.Text = emp.Phone;
                        txtEmail.Text = emp.Email;
                        txtEmpId.Text = emp.EmployeeID.ToString("D4"); // Например: 0001

                        txtPosition.Text = emp.Position != null ? emp.Position.PositionName : "Должность не указана";
                        txtLogin.Text = emp.Users != null ? emp.Users.Login : "Нет УЗ";

                        WelcomeText.Text = $"Добро пожаловать, {emp.Name} {emp.Patronymic}!";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки профиля: " + ex.Message);
            }
        }

        private void butEdit_Click(object sender, RoutedEventArgs e)
        {
            // Переносим текущие значения в поля ввода
            edPhone.Text = txtPhone.Text;
            edEmail.Text = txtEmail.Text;

            _isEditing = true;
            UpdateUI();
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AmurStoreEntities())
                {
                    var emp = db.Employees.Find(_currentEmployee.EmployeeID);
                    if (emp != null)
                    {
                        emp.Phone = edPhone.Text;
                        emp.Email = edEmail.Text;

                        db.SaveChanges();
                        MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        _isEditing = false;
                        LoadData(); // Перезагружаем отображение
                        UpdateUI();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void butCancel_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = false;
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Видимость текстовых полей vs полей ввода
            Visibility textVis = _isEditing ? Visibility.Collapsed : Visibility.Visible;
            Visibility editVis = _isEditing ? Visibility.Visible : Visibility.Collapsed;

            // Только телефон и email разрешено редактировать сотруднику
            txtPhone.Visibility = textVis;
            edPhone.Visibility = editVis;

            txtEmail.Visibility = textVis;
            edEmail.Visibility = editVis;

            // Кнопки
            butEdit.Visibility = textVis;
            butSave.Visibility = editVis;
            butCancel.Visibility = editVis;
        }
    }
}
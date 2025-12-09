using System;
using System.Data;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Amur_store.Views
{
    public partial class ProfilePage : Page
    {
        private int _currentClientId;
        private bool _isEditing = false;

        public ProfilePage(int clientId)
        {
            InitializeComponent();
            _currentClientId = clientId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadClientData();
        }

        private void LoadClientData()
        {
            try
            {
                // Загрузка данных клиента из базы данных
                // В реальном приложении используйте Entity Framework или другой ORM
                string query = $"SELECT * FROM Client WHERE id = {_currentClientId}";

                // Пример с использованием DataTable (замените на ваш метод доступа к данным)
                // DataTable clientData = DatabaseHelper.ExecuteQuery(query);

                // Если данных нет, показываем форму для заполнения
                // if (clientData.Rows.Count > 0)
                // {
                //     DataRow row = clientData.Rows[0];
                //     
                //     txtLogin.Text = row["login"]?.ToString() ?? "Не указано";
                //     txtEmail.Text = row["email"]?.ToString() ?? "Не указано";
                //     txtPhone_number.Text = row["phone_number"]?.ToString() ?? "Не указано";
                //     txtName.Text = row["name"]?.ToString() ?? "Не указано";
                //     txtSurname.Text = row["surname"]?.ToString() ?? "Не указано";
                //     txtPatronymic.Text = row["patronymic"]?.ToString() ?? "Не указано";
                //     txtAddress.Text = row["address"]?.ToString() ?? "Не указано";
                //     
                //     // Заполняем поля для редактирования
                //     edLogin.Text = txtLogin.Text;
                //     edEmail.Text = txtEmail.Text;
                //     edPhone_number.Text = txtPhone_number.Text;
                //     edName.Text = txtName.Text;
                //     edSurname.Text = txtSurname.Text;
                //     edPatronymic.Text = txtPatronymic.Text;
                //     edAddress.Text = txtAddress.Text;
                //     
                //     WelcomeText.Text = $"Добро пожаловать, {txtName.Text} {txtSurname.Text}!";
                // }
                // else
                // {
                //     // Создаем нового клиента
                //     WelcomeText.Text = "Заполните ваш профиль";
                // }

                // Временные данные для демонстрации
                txtLogin.Text = "user123";
                txtEmail.Text = "user@example.com";
                txtPhone_number.Text = "+7 (999) 150-66-77";
                txtName.Text = "Егор";
                txtSurname.Text = "Еремин";
                txtPatronymic.Text = "Владимирович";
                txtAddress.Text = "г. Москва, ул. Примерная, д. 123";

                edLogin.Text = txtLogin.Text;
                edEmail.Text = txtEmail.Text;
                edPhone_number.Text = txtPhone_number.Text;
                edName.Text = txtName.Text;
                edSurname.Text = txtSurname.Text;
                edPatronymic.Text = txtPatronymic.Text;
                edAddress.Text = txtAddress.Text;

                WelcomeText.Text = $"Добро пожаловать, {txtName.Text} {txtSurname.Text}!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void butEdit_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = true;
            UpdateUI();
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных
                if (string.IsNullOrWhiteSpace(edName.Text))
                {
                    MessageBox.Show("Поле 'Имя' обязательно для заполнения",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(edSurname.Text))
                {
                    MessageBox.Show("Поле 'Фамилия' обязательно для заполнения",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохранение данных в базу данных
                // string updateQuery = $@"
                //     UPDATE Client SET
                //         login = '{edLogin.Text}',
                //         email = '{edEmail.Text}',
                //         phone_number = '{edPhone_number.Text}',
                //         name = '{edName.Text}',
                //         surname = '{edSurname.Text}',
                //         patronymic = '{edPatronymic.Text}',
                //         address = '{edAddress.Text}'
                //     WHERE id = {_currentClientId}";
                // 
                // int rowsAffected = DatabaseHelper.ExecuteNonQuery(updateQuery);

                // Обновляем отображаемые данные
                txtLogin.Text = edLogin.Text;
                txtEmail.Text = edEmail.Text;
                txtPhone_number.Text = edPhone_number.Text;
                txtName.Text = edName.Text;
                txtSurname.Text = edSurname.Text;
                txtPatronymic.Text = edPatronymic.Text;
                txtAddress.Text = edAddress.Text;

                WelcomeText.Text = $"Добро пожаловать, {txtName.Text} {txtSurname.Text}!";

                _isEditing = false;
                UpdateUI();

                MessageBox.Show("Данные успешно сохранены!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void butCancel_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = false;
            UpdateUI();

            // Восстанавливаем исходные значения
            edLogin.Text = txtLogin.Text;
            edEmail.Text = txtEmail.Text;
            edPhone_number.Text = txtPhone_number.Text;
            edName.Text = txtName.Text;
            edSurname.Text = txtSurname.Text;
            edPatronymic.Text = txtPatronymic.Text;
            edAddress.Text = txtAddress.Text;
        }

        private void UpdateUI()
        {
            if (_isEditing)
            {
                // Переключаемся в режим редактирования
                txtLogin.Visibility = Visibility.Collapsed;
                txtEmail.Visibility = Visibility.Collapsed;
                txtPhone_number.Visibility = Visibility.Collapsed;
                txtName.Visibility = Visibility.Collapsed;
                txtSurname.Visibility = Visibility.Collapsed;
                txtPatronymic.Visibility = Visibility.Collapsed;
                txtAddress.Visibility = Visibility.Collapsed;

                edLogin.Visibility = Visibility.Visible;
                edEmail.Visibility = Visibility.Visible;
                edPhone_number.Visibility = Visibility.Visible;
                edName.Visibility = Visibility.Visible;
                edSurname.Visibility = Visibility.Visible;
                edPatronymic.Visibility = Visibility.Visible;
                edAddress.Visibility = Visibility.Visible;

                butEdit.Visibility = Visibility.Collapsed;
                butSave.Visibility = Visibility.Visible;
                butCancel.Visibility = Visibility.Visible;
                ViewOrdersButton.IsEnabled = false;
            }
            else
            {
                // Переключаемся в режим просмотра
                txtLogin.Visibility = Visibility.Visible;
                txtEmail.Visibility = Visibility.Visible;
                txtPhone_number.Visibility = Visibility.Visible;
                txtName.Visibility = Visibility.Visible;
                txtSurname.Visibility = Visibility.Visible;
                txtPatronymic.Visibility = Visibility.Visible;
                txtAddress.Visibility = Visibility.Visible;

                edLogin.Visibility = Visibility.Collapsed;
                edEmail.Visibility = Visibility.Collapsed;
                edPhone_number.Visibility = Visibility.Collapsed;
                edName.Visibility = Visibility.Collapsed;
                edSurname.Visibility = Visibility.Collapsed;
                edPatronymic.Visibility = Visibility.Collapsed;
                edAddress.Visibility = Visibility.Collapsed;

                butEdit.Visibility = Visibility.Visible;
                butSave.Visibility = Visibility.Collapsed;
                butCancel.Visibility = Visibility.Collapsed;
                ViewOrdersButton.IsEnabled = true;
            }
        }

        private void ViewOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу истории заказов
            MessageBox.Show("Переход на страницу истории заказов",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

            // Пример:
            // NavigationService.Navigate(new OrdersPage(_currentClientId));
        }
    }
}
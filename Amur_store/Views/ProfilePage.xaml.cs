using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Amur_store; // Твое пространство имен

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
                using (var db = new AmurStoreEntities())
                {
                    // 1. Находим клиента по ClientID
                    var client = db.Clients.Find(_currentClientId);

                    if (client != null)
                    {
                        // 2. Данные из таблицы Clients
                        txtEmail.Text = client.Email ?? "—";
                        txtPhone.Text = client.Phone ?? "—";
                        txtAddress.Text = client.Address ?? "—";

                        txtName.Text = client.Name ?? "—";
                        txtSurname.Text = client.Surname ?? "—";
                        txtPatronymic.Text = client.Patronymic ?? "—";

                        WelcomeText.Text = $"Здравствуйте, {client.Name}!";

                        // 3. Данные из таблицы Users (Логин)
                        // Ищем пользователя, связанного с этим клиентом
                        var user = db.Users.Find(client.UserID);
                        if (user != null)
                        {
                            txtLogin.Text = user.Login;
                        }
                    }
                    else
                    {
                        WelcomeText.Text = "Профиль не найден";
                        butEdit.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void butEdit_Click(object sender, RoutedEventArgs e)
        {
            // КОПИРУЕМ данные в поля ввода (если там прочерк, то пустоту)
            edLogin.Text = txtLogin.Text == "—" ? "" : txtLogin.Text;
            edEmail.Text = txtEmail.Text == "—" ? "" : txtEmail.Text;
            edPhone.Text = txtPhone.Text == "—" ? "" : txtPhone.Text;
            edAddress.Text = txtAddress.Text == "—" ? "" : txtAddress.Text;
            edName.Text = txtName.Text == "—" ? "" : txtName.Text;
            edSurname.Text = txtSurname.Text == "—" ? "" : txtSurname.Text;
            edPatronymic.Text = txtPatronymic.Text == "—" ? "" : txtPatronymic.Text;

            _isEditing = true;
            UpdateUI();
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AmurStoreEntities())
                {
                    var client = db.Clients.Find(_currentClientId);
                    if (client != null)
                    {
                        // Обновляем данные клиента
                        client.Email = edEmail.Text;
                        client.Phone = edPhone.Text;
                        client.Address = edAddress.Text;
                        client.Name = edName.Text;
                        client.Surname = edSurname.Text;
                        client.Patronymic = edPatronymic.Text;

                        // Обновляем логин в таблице Users
                        var user = db.Users.Find(client.UserID);
                        if (user != null)
                        {
                            user.Login = edLogin.Text;
                        }

                        db.SaveChanges(); // Сохраняем в БД

                        MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        _isEditing = false;
                        LoadClientData(); // Обновляем текст на экране
                        UpdateUI();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void butCancel_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = false;
            UpdateUI();
        }

        private void UpdateUI()
        {
            Visibility textVis = _isEditing ? Visibility.Collapsed : Visibility.Visible;
            Visibility editVis = _isEditing ? Visibility.Visible : Visibility.Collapsed;

            // Переключение видимости полей
            txtLogin.Visibility = textVis; txtEmail.Visibility = textVis;
            txtPhone.Visibility = textVis; txtAddress.Visibility = textVis;
            txtName.Visibility = textVis; txtSurname.Visibility = textVis;
            txtPatronymic.Visibility = textVis;

            edLogin.Visibility = editVis; edEmail.Visibility = editVis;
            edPhone.Visibility = editVis; edAddress.Visibility = editVis;
            edName.Visibility = editVis; edSurname.Visibility = editVis;
            edPatronymic.Visibility = editVis;

            butEdit.Visibility = textVis;
            butSave.Visibility = editVis;
            butCancel.Visibility = editVis;

            ViewOrdersButton.IsEnabled = !_isEditing;
        }

        private void ViewOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            // Переход к заказам
            NavigationService.Navigate(new OrderPage(_currentClientId));
        }
    }
}
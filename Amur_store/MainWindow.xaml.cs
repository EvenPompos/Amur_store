using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;

// Мы явно не используем 'using Amur_store.Views;' или 'using Amur_store.Pages;' для классов страниц,
// чтобы писать их с префиксом и избежать путаницы.

namespace Amur_store
{
    public partial class MainWindow : Window
    {
        private int userId;
        private string userName;
        private int userRole;

        public MainWindow(int userId, string userName, int roleId)
        {
            InitializeComponent();
            this.userId = userId;
            this.userName = userName;
            this.userRole = roleId;

            Title = $"Amur Store - {userName}";

            ConfigureInterface();
            LoadHomePage();
        }

        private void ConfigureInterface()
        {
            // Сброс видимости
            if (SidebarBasketText != null) SidebarBasketText.Visibility = Visibility.Visible;
            if (butBasket != null) butBasket.Visibility = Visibility.Visible;
            if (SidebarEmployeesPanel != null) SidebarEmployeesPanel.Visibility = Visibility.Collapsed;

            // Если СОТРУДНИК (2) или АДМИН (1)
            if (userRole == 1 || userRole == 2)
            {
                if (butBasket != null) butBasket.Visibility = Visibility.Collapsed;
                if (SidebarBasketText != null) SidebarBasketText.Visibility = Visibility.Collapsed;
                if (SidebarOrdersText != null) SidebarOrdersText.Text = "Работа";

                // Только для Админа
                if (userRole == 1 && SidebarEmployeesPanel != null)
                {
                    SidebarEmployeesPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void LoadHomePage()
        {
            if (userRole == 3)
            {
                // Главная клиента
                MainContent.Navigate(new Amur_store.Views.MainPage());
            }
            else
            {
                // Главная сотрудника -> Заказы
                butOrders_Click(null, null);
            }
        }

        // --- НАВИГАЦИЯ ---

        private void butHome_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Navigate(new Amur_store.Views.MainPage());
        }

        private void butAccount_Click(object sender, RoutedEventArgs e)
        {
            if (userRole == 3) // КЛИЕНТ
            {
                // Открываем обычный профиль (принимает ID)
                MainContent.Navigate(new Amur_store.Views.ProfilePage(userId));
            }
            else // СОТРУДНИК / АДМИН
            {
                using (var db = new AmurStoreEntities())
                {
                    var emp = db.Employees.Find(userId);
                    if (emp != null)
                    {
                        // ВАЖНО: Открываем EmployeeProfilePage (принимает объект Employees)
                        MainContent.Navigate(new Amur_store.Views.EmployeeProfilePage(emp));
                    }
                }
            }
        }

        private void butCatalog_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Navigate(new Amur_store.Views.CatalogPage());
        }

        private void butBasket_Click(object sender, RoutedEventArgs e)
        {
            if (userRole == 3)
            {
                MainContent.Navigate(new Amur_store.Views.CartPage(userId));
            }
        }

        // === ИСПРАВЛЕНИЕ ОШИБКИ ЗДЕСЬ ===
        private void butOrders_Click(object sender, RoutedEventArgs e)
        {
            if (userRole == 3)
            {
                // Клиент смотрит свои покупки (принимает int)
                MainContent.Navigate(new Amur_store.Views.OrderPage(userId));
            }
            else
            {
                using (var db = new AmurStoreEntities())
                {
                    var emp = db.Employees.Find(userId);
                    if (emp != null)
                    {
                        // Сотрудник смотрит рабочие заказы (принимает Employees)
                        MainContent.Navigate(new Amur_store.Views.WorkOrdersPage(emp));
                    }
                }
            }
        }

        private void butEmployees_Click(object sender, RoutedEventArgs e)
        {
            if (userRole == 1)
            {
                // Страница добавления (Pages)
                MainContent.Navigate(new Amur_store.Views.AddEmployeePage());
            }
        }

        private void butPowerOff_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (userRole == 3) new SignIn().Show();
                else new EmployeeSignIn().Show();

                this.Close();
            }
        }

        private void btnUserProfile_Click(object sender, RoutedEventArgs e) => butAccount_Click(sender, e);

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) PerformSearch(txtSearch.Text.Trim());
        }

        private void PerformSearch(string query)
        {
            MessageBox.Show($"Поиск: {query}");
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scale = Math.Min(this.ActualWidth / 1050, this.ActualHeight / 600);
            if (MainGrid != null)
            {
                foreach (var element in MainGrid.Children)
                {
                    if (element is FrameworkElement fe) fe.LayoutTransform = new ScaleTransform(scale, scale);
                }
            }
        }
    }
}
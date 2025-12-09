using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Amur_store
{
    public partial class MainWindow : Window
    {
        private int userId;
        private string userName;

        public MainWindow(int userId, string userName)
        {
            InitializeComponent();
            this.userId = userId;
            this.userName = userName;

            // Установка заголовка
            Title = $"Amur Store - {userName}";

            // Загрузка главной страницы по умолчанию
            LoadHomePage();
        }

        private void LoadHomePage()
        {
            // Загрузка страницы "Главная"
            MainContent.Navigate(new Views.MainPage());
        }

        // Обработчики событий для кнопок навигации
        private void butHome_Click(object sender, RoutedEventArgs e)
        {
            // Загрузка главной страницы
            LoadHomePage();
        }

        private void butAccount_Click(object sender, RoutedEventArgs e)
        {
            // Загрузка страницы профиля
            MainContent.Navigate(new Views.ProfilePage(userId));
        }

        private void butCatalog_Click(object sender, RoutedEventArgs e)
        {
            // Загрузка каталога товаров
            // MainContent.Navigate(new CatalogPage(userId));
        }

        private void butBasket_Click(object sender, RoutedEventArgs e)
        {
            // Загрузка корзины
            // MainContent.Navigate(new BasketPage(userId));
        }

        private void butOrders_Click(object sender, RoutedEventArgs e)
        {
            // Загрузка истории заказов
            MainContent.Navigate(new Views.OrdersPage(userId));
        }

        private void butPowerOff_Click(object sender, RoutedEventArgs e)
        {
            // Выход из системы
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SignIn signInWindow = new SignIn();
                signInWindow.Show();
                this.Close();
            }
        }

        // Обработка поиска
        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string searchText = txtSearch.Text.Trim();
                if (!string.IsNullOrEmpty(searchText))
                {
                    // Выполнение поиска
                    PerformSearch(searchText);
                }
            }
        }

        private void PerformSearch(string searchQuery)
        {
            // Реализация поиска товаров
            MessageBox.Show($"Выполняется поиск: {searchQuery}",
                "Поиск", MessageBoxButton.OK, MessageBoxImage.Information);

            // Пример: MainContent.Navigate(new SearchResultsPage(searchQuery, userId));
        }

        // Обработчики для верхних кнопок
        private void btnUserProfile_Click(object sender, RoutedEventArgs e)
        {
            butAccount_Click(sender, e); // Перенаправление на страницу профиля
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Адаптация интерфейса при изменении размера окна
            double scaleX = this.ActualWidth / 1050;
            double scaleY = this.ActualHeight / 600;
            double scale = Math.Min(scaleX, scaleY);

            foreach (var element in MainGrid.Children)
            {
                if (element is FrameworkElement fe)
                {
                    fe.LayoutTransform = new ScaleTransform(scale, scale);
                }
            }
        }
    }
}
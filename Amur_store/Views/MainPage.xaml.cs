using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Amur_store.Views
{
    public partial class MainPage : Page
    {
        private List<Product> _allProducts;
        private Random _random = new Random();

        public MainPage()
        {
            InitializeComponent();
            LoadProducts();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ShowRandomProducts();
        }

        private void LoadProducts()
        {
            // Загружаем все продукты из базы данных
            _allProducts = new List<Product>
            {
                new Product { Id = 3, Name = "Amur Нарвал A5А12", Price = 35000, ImagePath = "/Resources/Images/PC/Amur Нарвал A5А12/PC_A5A12_v1.png" },
                new Product { Id = 4, Name = "AMUR Тигр H6I12", Price = 45000, ImagePath = "/Resources/Images/Monoblock/AMUR Тигр H6I12/Monoblock_H6l12_v1.png" },
                new Product { Id = 5, Name = "AMUR Финвал H6I12", Price = 29000, ImagePath = "/Resources/Images/Nettop/AMUR Финвал H6I12/Nettop_H6l12_v1.png" },
                new Product { Id = 6, Name = "AMUR Нарвал A5А14 MT", Price = 30000, ImagePath = "/Resources/Images/PC/Amur Нарвал A5А14 MT/PC_A5A14_MT_v1.png" },
                // Добавьте остальные продукты по аналогии
                new Product { Id = 8, Name = "IRU Home 510B7SE", Price = 33000, ImagePath = "/Resources/Images/PC/IRU Home 510B7SE/IRU Home 510B7SE_v1.png" },
                new Product { Id = 56, Name = "Видеокарта Asus RTX5060Ti", Price = 56800, ImagePath = "/Resources/Images/GPU/Видеокарта Asus PCI-E 4.0 DUAL-RTX5060TI-O16G/Видеокарта Asus PCI-E 4.0 DUAL-RTX5060TI-O16G_v1.jpg" },
                new Product { Id = 59, Name = "Видеокарта MSI RTX 5060 Ti", Price = 51999, ImagePath = "/Resources/Images/GPU/Видеокарта MSI GeForce RTX 5060 Ti VENTUS 2X OC PLUS/Видеокарта MSI GeForce RTX 5060 Ti VENTUS 2X OC PLUS_v1.jpg" },
                new Product { Id = 30, Name = "Процессор AMD Ryzen 5 5600", Price = 9299, ImagePath = "/Resources/Images/CPU/Процессор AMD Ryzen 5 5600 OEM/Процессор AMD Ryzen 5 5600 OEM_v1.jpg" },
                new Product { Id = 80, Name = "Ноутбук «Гравитон» Н15А-Б", Price = 77000, ImagePath = "/Resources/Images/Laptop/Ноутбук «Гравитон» Н15А-Б/Ноутбук «Гравитон» Н15А-Б_v1.jpg" }
            };

            // В реальном приложении загружайте продукты из базы данных:
            // using (var context = new YourDbContext())
            // {
            //     _allProducts = context.Products.ToList();
            // }
        }

        private void ShowRandomProducts()
        {
            if (_allProducts == null || _allProducts.Count < 2)
                return;

            // Выбираем 3 случайных продукта
            var randomProducts = _allProducts
                .OrderBy(x => _random.Next())
                .Take(2)
                .ToList();

            RandomProductsContainer.ItemsSource = randomProducts;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomProducts();
        }

        private void ProductDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int productId)
            {
                // Переход на страницу деталей товара
                // NavigationService.Navigate(new ProductDetailsPage(productId));

                MessageBox.Show($"Открываем детали товара с ID: {productId}",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SocialButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string url)
            {
                try
                {
                    // Открытие ссылки в браузере
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии ссылки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // Вспомогательный класс продукта (в реальном приложении используйте модель из БД)
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImagePath { get; set; }
    }
}
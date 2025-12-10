using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amur_store.Models;

namespace Amur_store.Views
{
    public partial class CatalogPage : Page
    {
        private List<ProductViewModel> allProducts = new List<ProductViewModel>();

        public CatalogPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCatalogData();
        }

        private void LoadCatalogData()
        {
            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // Загружаем все товары с категориями и производителями
                    allProducts.Clear();

                    var products = db.Products.ToList();
                    foreach (var product in products)
                    {
                        var viewModel = new ProductViewModel
                        {
                            ProductID = product.ProductID,
                            ProductName = product.ProductName,
                            Price = product.Price,
                            ImagePath = product.ImagePath
                        };

                        // Загружаем категорию
                        if (product.CategoryID != null)
                        {
                            var category = db.Categories.Find(product.CategoryID);
                            viewModel.CategoryName = category?.CategoryName ?? "Не указана";
                        }

                        // Загружаем производителя
                        if (product.ManufacturyID != null)
                        {
                            var manufacturer = db.Manufactures.Find(product.ManufacturyID);
                            viewModel.ManufacturerName = manufacturer?.ManufacturyName ?? "Не указан";
                        }

                        allProducts.Add(viewModel);
                    }

                    // Показываем все товары
                    ProductsDataGrid.ItemsSource = allProducts;
                    CatalogInfoText.Text = $"Товаров в каталоге: {allProducts.Count}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки каталога: {ex.Message}");
            }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out int productId))
                {
                    var product = allProducts.FirstOrDefault(p => p.ProductID == productId);
                    if (product != null)
                    {
                        // Здесь можно добавить логику добавления в корзину
                        // Например, сохранить в сессии или временном хранилище
                        MessageBox.Show($"Товар '{product.ProductName}' добавлен в корзину",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Пример сохранения в статическом классе (можно заменить на реальную логику)
                        AddToCart(productId, product.ProductName, product.Price);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // Простой метод для демонстрации добавления в корзину
        private void AddToCart(int productId, string productName, decimal price)
        {
            // В реальном приложении здесь будет логика добавления в корзину
            // Например, сохранение в БД или в статической переменной

            // Пример временного хранения (можно заменить на реальное)
            if (!App.CartItems.ContainsKey(productId))
            {
                App.CartItems[productId] = new CartItem
                {
                    ProductID = productId,
                    ProductName = productName,
                    Price = price,
                    Quantity = 1
                };
            }
            else
            {
                App.CartItems[productId].Quantity++;
            }
        }
    }

    // Классы для отображения
    public class ProductViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string ManufacturerName { get; set; }
        public decimal Price { get; set; }
        public string ImagePath { get; set; }
    }

    // Простой класс для корзины (временное хранение)
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }

    // Статический класс для хранения корзины (можно заменить на реальное хранилище)
    public static class App
    {
        public static Dictionary<int, CartItem> CartItems { get; } = new Dictionary<int, CartItem>();
    }
}
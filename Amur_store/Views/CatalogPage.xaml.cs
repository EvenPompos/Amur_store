using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity; // Обязательно для работы .Include()

// ВАЖНО: Эта строка подключает твои классы базы данных (Products, Manufactures...)
using Amur_store;

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
                // ВАЖНО: Если AmurStoreEntities подчеркнуто красным, открой файл Model1.Context.cs
                // и посмотри точное название класса (часто бывает Amur_storeEntities).
                // Вставь правильное имя сюда вместо AmurStoreEntities.
                using (var db = new AmurStoreEntities())
                {
                    allProducts.Clear();

                    // 1. Используем имя Products (так называется класс в твоем коде)
                    // 2. Используем .Categories (так называется связь в твоем коде)
                    var productsList = db.Products
                        .Include(p => p.Manufactures)
                        .Include(p => p.Manufactures.Categories)
                        .ToList();

                    foreach (var product in productsList)
                    {
                        var viewModel = new ProductViewModel
                        {
                            ProductID = product.ProductID,
                            ProductName = product.ProductName,
                            Description = product.Description,

                            // Convert.ToDecimal универсален: работает с int, double и null
                            Price = Convert.ToDecimal(product.Price),

                            // Заглушка, если картинки нет
                            ImagePath = string.IsNullOrEmpty(product.ImagePath) ? "/Resources/default.png" : product.ImagePath
                        };

                        // Логика получения производителя
                        if (product.Manufactures != null)
                        {
                            viewModel.ManufacturerName = product.Manufactures.ManufacturyName;

                            // ИСПРАВЛЕНО: Обращаемся к .Categories (как в твоем файле)
                            if (product.Manufactures.Categories != null)
                            {
                                viewModel.CategoryName = product.Manufactures.Categories.CategoryName;
                            }
                            else
                            {
                                viewModel.CategoryName = "Не указана";
                            }
                        }
                        else
                        {
                            viewModel.ManufacturerName = "—";
                            viewModel.CategoryName = "—";
                        }

                        allProducts.Add(viewModel);
                    }

                    // Обновляем таблицу
                    ProductsDataGrid.ItemsSource = null;
                    ProductsDataGrid.ItemsSource = allProducts;

                    CatalogInfoText.Text = $"Товаров в каталоге: {allProducts.Count}";
                }
            }
            catch (Exception ex)
            {
                // Выводим подробную ошибку для диагностики
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Ошибка загрузки: {msg}");
            }
        }

        // --- Кнопка добавления в корзину ---
        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    if (int.TryParse(button.Tag.ToString(), out int productId))
                    {
                        var product = allProducts.FirstOrDefault(p => p.ProductID == productId);
                        if (product != null)
                        {
                            // Добавляем в локальную корзину
                            GlobalBasket.Add(productId, product.ProductName, product.Price);

                            MessageBox.Show($"Товар '{product.ProductName}' добавлен в корзину!",
                                "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }

    // ViewModel для отображения
    public class ProductViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string ManufacturerName { get; set; }
        public decimal Price { get; set; }
        public string ImagePath { get; set; }
        public string Description { get; set; }
    }

    // === КОРЗИНА ===
    // Назвал GlobalBasket, чтобы не конфликтовало с другими файлами
    public static class GlobalBasket
    {
        public static Dictionary<int, BasketItem> Items { get; } = new Dictionary<int, BasketItem>();

        public static void Add(int productId, string name, decimal price)
        {
            if (Items.ContainsKey(productId))
            {
                Items[productId].Quantity++;
            }
            else
            {
                Items[productId] = new BasketItem
                {
                    ProductID = productId,
                    ProductName = name,
                    Price = price,
                    Quantity = 1
                };
            }
        }
    }

    public class BasketItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }
}
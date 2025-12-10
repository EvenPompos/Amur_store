using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amur_store.Models;

namespace Amur_store.Views
{
    public partial class CartPage : Page
    {
        private int clientId;
        private List<CartItem> cartItems = new List<CartItem>();

        public CartPage(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;

            // Устанавливаем доставку по умолчанию
            DeliveryComboBox.SelectedIndex = 0;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCartData();
        }

        private void LoadCartData()
        {
            try
            {
                // Заглушка: в реальном приложении здесь будет загрузка из БД или сессии
                // Для демонстрации создаем тестовые данные
                cartItems.Clear();

                // Пример тестовых данных
                cartItems.Add(new CartItem
                {
                    ProductID = 3,
                    ProductName = "Amur Нарвал A5А12",
                    Price = 31500,
                    Quantity = 1
                });

                cartItems.Add(new CartItem
                {
                    ProductID = 30,
                    ProductName = "Процессор AMD Ryzen 5 5600 OEM",
                    Price = 7904.15m,
                    Quantity = 1
                });

                cartItems.Add(new CartItem
                {
                    ProductID = 40,
                    ProductName = "Оперативная память Digma DGMAD43200016D 16 ГБ",
                    Price = 3599.10m,
                    Quantity = 2
                });

                UpdateCartDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки корзины: {ex.Message}");
            }
        }

        private void UpdateCartDisplay()
        {
            if (cartItems.Count == 0)
            {
                // Корзина пуста
                CartDataGrid.Visibility = Visibility.Collapsed;
                EmptyCartPanel.Visibility = Visibility.Visible;
                CartInfoText.Text = "Корзина пуста";
            }
            else
            {
                // Корзина не пуста
                CartDataGrid.Visibility = Visibility.Visible;
                EmptyCartPanel.Visibility = Visibility.Collapsed;
                CartInfoText.Text = $"Товаров в корзине: {cartItems.Sum(i => i.Quantity)}";

                // Обновляем DataGrid
                CartDataGrid.ItemsSource = cartItems;

                // Обновляем суммы
                CalculateTotals();
            }
        }

        private void CalculateTotals()
        {
            decimal subtotal = cartItems.Sum(item => item.Total);
            decimal deliveryCost = GetDeliveryCost();
            decimal total = subtotal + deliveryCost;

            SubtotalText.Text = $"{subtotal:N0} руб.";
            DeliveryCostText.Text = $"{deliveryCost:N0} руб.";
            TotalText.Text = $"{total:N0} руб.";
        }

        private decimal GetDeliveryCost()
        {
            if (DeliveryComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Tag != null &&
                decimal.TryParse(selectedItem.Tag.ToString(), out decimal cost))
            {
                return cost;
            }
            return 0;
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out int productId))
            {
                // Удаляем товар из корзины
                var itemToRemove = cartItems.FirstOrDefault(item => item.ProductID == productId);
                if (itemToRemove != null)
                {
                    cartItems.Remove(itemToRemove);
                    UpdateCartDisplay();

                    MessageBox.Show("Товар удален из корзины");
                }
            }
        }

        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count > 0)
            {
                var result = MessageBox.Show("Очистить корзину?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    cartItems.Clear();
                    UpdateCartDisplay();
                    MessageBox.Show("Корзина очищена");
                }
            }
        }

        private void DeliveryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cartItems.Count > 0)
            {
                CalculateTotals();
            }
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста");
                return;
            }

            try
            {
                // В реальном приложении здесь будет сохранение заказа в БД
                using (var db = new AmurStoreEntities())
                {
                    // Создаем новый заказ
                    var order = new Orders
                    {
                        ClientID = clientId,
                        OrderDate = DateTime.Now,
                        DeliveryID = DeliveryComboBox.SelectedIndex + 1, // Предполагаем, что ID начинаются с 1
                        WarehouseID = 1, // Склад по умолчанию
                        PaymentTypeID = 1, // Картой
                        PaymentStatusID = 2, // Не оплачен
                        OrderStatusID = 1, // Оформлен
                        TotalAmount = cartItems.Sum(item => item.Total),
                        DiscountApplied = 0,
                        FinalAmount = cartItems.Sum(item => item.Total) + GetDeliveryCost()
                    };

                    db.Orders.Add(order);
                    db.SaveChanges();

                    // Добавляем детали заказа
                    foreach (var item in cartItems)
                    {
                        var orderDetail = new OrderDetails
                        {
                            OrderID = order.OrderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price,
                            Subtotal = item.Total
                        };

                        db.OrderDetails.Add(orderDetail);
                    }

                    db.SaveChanges();

                    // Очищаем корзину
                    cartItems.Clear();
                    UpdateCartDisplay();

                    MessageBox.Show($"Заказ №{order.OrderID} успешно оформлен!\nСумма: {order.FinalAmount:N0} руб.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления заказа: {ex.Message}", "Ошибка");
            }
        }

        private void GoToCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            // Навигация на страницу каталога
            // В реальном приложении здесь будет NavigationService.Navigate()
            MessageBox.Show("Переход в каталог");
        }
    }

    // Модель для отображения товаров в корзине
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }
}
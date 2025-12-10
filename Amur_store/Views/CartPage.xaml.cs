using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation; // Для навигации назад
using Amur_store; // Подключаем пространство имен с классами базы (Orders, OrderDetails)

namespace Amur_store.Views
{
    public partial class CartPage : Page
    {
        private int currentClientId;

        // --- ВАЖНО: 1. Пустой конструктор (чтобы не было ошибки при открытии) ---
        public CartPage()
        {
            InitializeComponent();
            this.currentClientId = 1; // ID клиента по умолчанию (для тестов)
        }

        // --- 2. Конструктор с параметром (если передаем ID при входе) ---
        public CartPage(int clientId)
        {
            InitializeComponent();
            this.currentClientId = clientId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCartDisplay();
        }

        // Обновление экрана
        private void UpdateCartDisplay()
        {
            try
            {
                // БЕРЕМ ДАННЫЕ ИЗ ГЛОБАЛЬНОЙ КОРЗИНЫ (созданной в CatalogPage)
                var items = GlobalBasket.Items.Values.ToList();

                if (items.Count == 0)
                {
                    CartContentPanel.Visibility = Visibility.Collapsed;
                    EmptyCartPanel.Visibility = Visibility.Visible;
                    CartInfoText.Text = "Корзина пуста";
                }
                else
                {
                    CartContentPanel.Visibility = Visibility.Visible;
                    EmptyCartPanel.Visibility = Visibility.Collapsed;
                    CartInfoText.Text = $"Товаров в корзине: {items.Sum(i => i.Quantity)}";

                    // Обновляем таблицу
                    CartDataGrid.ItemsSource = null;
                    CartDataGrid.ItemsSource = items;

                    CalculateTotals(items);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // Подсчет суммы
        private void CalculateTotals(List<BasketItem> items)
        {
            decimal subtotal = items.Sum(item => item.Total);
            decimal deliveryCost = GetDeliveryCost();
            decimal total = subtotal + deliveryCost;

            SubtotalText.Text = $"{subtotal:N0} ₽";
            DeliveryCostText.Text = $"{deliveryCost:N0} ₽";
            TotalText.Text = $"{total:N0} ₽";
        }

        private decimal GetDeliveryCost()
        {
            if (DeliveryComboBox.SelectedItem is ComboBoxItem item &&
                item.Tag != null &&
                decimal.TryParse(item.Tag.ToString(), out decimal cost))
            {
                return cost;
            }
            return 0;
        }

        // --- Удаление товара ---
        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out int productId))
            {
                // Удаляем из глобальной памяти
                if (GlobalBasket.Items.ContainsKey(productId))
                {
                    GlobalBasket.Items.Remove(productId);
                    UpdateCartDisplay(); // Обновляем вид
                }
            }
        }

        // --- Очистка корзины ---
        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalBasket.Items.Count > 0)
            {
                if (MessageBox.Show("Очистить корзину полностью?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    GlobalBasket.Items.Clear();
                    UpdateCartDisplay();
                }
            }
        }

        private void DeliveryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded) UpdateCartDisplay();
        }

        private void GoToCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат назад (если перешли через Frame)
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        // --- ОФОРМЛЕНИЕ ЗАКАЗА ---
        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalBasket.Items.Count == 0) return;

            try
            {
                using (var db = new AmurStoreEntities())
                {
                    var items = GlobalBasket.Items.Values.ToList();
                    decimal productsTotal = items.Sum(i => i.Total);
                    decimal deliveryCost = GetDeliveryCost();

                    // 1. Создаем заказ
                    var newOrder = new Orders
                    {
                        ClientID = currentClientId,
                        OrderDate = DateTime.Now,

                        // Берем ID доставки из выпадающего списка (+1, т.к. индексы с 0)
                        // В реальном проекте лучше искать ID по имени
                        DeliveryID = DeliveryComboBox.SelectedIndex + 1,

                        WarehouseID = 1,     // Склад по умолчанию
                        PaymentTypeID = 1,   // Тип оплаты (Карта/Нал)
                        PaymentStatusID = 1, // Статус оплаты (Не оплачен)
                        OrderStatusID = 1,   // Статус заказа (Новый)

                        TotalAmount = productsTotal,
                        DiscountApplied = 0,
                        FinalAmount = productsTotal + deliveryCost
                    };

                    db.Orders.Add(newOrder);
                    db.SaveChanges(); // Сохраняем, чтобы получить OrderID

                    // 2. Добавляем детали (товары)
                    foreach (var item in items)
                    {
                        var detail = new OrderDetails
                        {
                            OrderID = newOrder.OrderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price,
                            Subtotal = item.Total
                        };
                        db.OrderDetails.Add(detail);
                    }

                    db.SaveChanges();

                    // 3. Успех
                    MessageBox.Show($"Заказ №{newOrder.OrderID} успешно оформлен!",
                        "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                    GlobalBasket.Items.Clear();
                    UpdateCartDisplay();
                }
            }
            catch (Exception ex)
            {
                // Показываем внутреннюю ошибку, если есть (Entity Framework часто прячет суть там)
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Ошибка при оформлении: {msg}", "Ошибка БД");
            }
        }
    }
}
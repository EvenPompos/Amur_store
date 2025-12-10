using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Amur_store.Views
{
    public partial class OrderPage : Page
    {
        private int clientId;

        public OrderPage(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;

            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Now;
            StatusFilterComboBox.SelectedIndex = 0;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // Загружаем заказы клиента
                    var orders = db.Orders
                        .Where(o => o.ClientID == clientId)
                        .OrderByDescending(o => o.OrderDate)
                        .ToList();

                    // Загружаем связанные данные для каждого заказа
                    foreach (var order in orders)
                    {
                        db.Entry(order).Reference("Delivery").Load();
                        db.Entry(order).Reference("PaymentType").Load();
                        db.Entry(order).Reference("OrderStatus").Load();
                        db.Entry(order).Reference("PaymentStatus").Load();
                    }

                    ApplyFilters(orders);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void ApplyFilters(System.Collections.Generic.List<Orders> orders)
        {
            try
            {
                var filteredOrders = orders.AsEnumerable();

                // Фильтр по статусу
                if (StatusFilterComboBox.SelectedItem is ComboBoxItem item &&
                    item.Tag != null &&
                    item.Tag.ToString() != "0")
                {
                    string statusName = "";
                    switch (item.Tag.ToString())
                    {
                        case "1": statusName = "Оформлен"; break;
                        case "2": statusName = "В пути"; break;
                        case "3": statusName = "Доставлен"; break;
                        case "4": statusName = "Отменен"; break;
                    }

                    filteredOrders = filteredOrders.Where(o =>
                        o.OrderStatusID != null);
                }

                // Фильтр по дате
                if (StartDatePicker.SelectedDate.HasValue)
                    filteredOrders = filteredOrders.Where(o => o.OrderDate >= StartDatePicker.SelectedDate.Value);

                if (EndDatePicker.SelectedDate.HasValue)
                    filteredOrders = filteredOrders.Where(o => o.OrderDate <= EndDatePicker.SelectedDate.Value.AddDays(1));

                OrdersDataGrid.ItemsSource = filteredOrders.ToList();
                OrdersInfoText.Text = $"Заказов: {filteredOrders.Count()}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}");
            }
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadData();
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            StatusFilterComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Now;
            LoadData();
        }

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int orderId))
            {
                ShowOrderDetails(orderId);
            }
        }

        private void ShowOrderDetails(int orderId)
        {
            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // Детали заказа
                    var details = db.OrderDetails
                        .Where(od => od.OrderID == orderId)
                        .ToList();

                    // Загружаем информацию о продуктах
                    foreach (var detail in details)
                    {
                        db.Entry(detail).Reference("Product").Load();
                    }

                    OrderDetailsDataGrid.ItemsSource = details;

                    // Информация о заказе
                    var order = db.Orders.Find(orderId);
                    if (order != null)
                    {
                        db.Entry(order).Reference("Delivery").Load();

                        SelectedOrderNumber.Text = order.OrderID.ToString();

                        decimal itemsTotal = details.Sum(d => d.Subtotal ?? 0);
                        decimal deliveryCost = order.DeliveryID;
                        decimal discount = order.DiscountApplied ?? 0;
                        decimal discountAmount = itemsTotal * discount / 100;
                        decimal final = order.FinalAmount ?? itemsTotal + deliveryCost - discountAmount;

                        ItemsTotalText.Text = $"{itemsTotal:N0} руб.";
                        DeliveryCostText.Text = $"{deliveryCost:N0} руб.";
                        DiscountText.Text = discount > 0 ? $"-{discount}% ({discountAmount:N0} руб.)" : "0% (0 руб.)";
                        FinalAmountText.Text = $"{final:N0} руб.";
                    }

                    OrderDetailsPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка деталей: {ex.Message}");
            }
        }

        private void CloseDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            OrderDetailsPanel.Visibility = Visibility.Collapsed;
        }
    }
}
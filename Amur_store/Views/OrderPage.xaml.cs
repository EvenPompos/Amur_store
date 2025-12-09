using Amur_store;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Amur_store.Views
{
    public partial class OrderPage : Page
    {
        private int _currentClientId;
        private List<Order> _allOrders;
        private AmurStoreEntities _context;

        public OrderPage(int clientId)
        {
            InitializeComponent();
            _currentClientId = clientId;
            _context = new AmurStoreEntities();

            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Now;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                _context = new AmurStoreEntities();

                _allOrders = _context.Orders
                    .Where(o => o.ClientID == _currentClientId)
                    .Include(o => o.Delivery)
                    .Include(o => o.PaymentType)
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentStatus)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                foreach (var order in _allOrders)
                {
                    order.CanCancel = order.OrderStatusID == 1 || order.OrderStatusID == 2;
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allOrders == null) return;

            IEnumerable<Order> filteredOrders = _allOrders;

            if (StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                int statusId = int.Parse(selectedItem.Tag.ToString());
                if (statusId > 0)
                {
                    filteredOrders = filteredOrders.Where(o => o.OrderStatusID == statusId);
                }
            }

            if (StartDatePicker.SelectedDate.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.OrderDate >= StartDatePicker.SelectedDate.Value);
            }

            if (EndDatePicker.SelectedDate.HasValue)
            {
                var endDate = EndDatePicker.SelectedDate.Value.AddDays(1);
                filteredOrders = filteredOrders.Where(o => o.OrderDate < endDate);
            }

            OrdersDataGrid.ItemsSource = filteredOrders.ToList();
            OrdersInfoText.Text = $"Найдено заказов: {filteredOrders.Count()}";
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            StatusFilterComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Now;
            ApplyFilters();
        }

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                LoadOrderDetails(orderId);
            }
        }

        private void LoadOrderDetails(int orderId)
        {
            try
            {
                _context = new AmurStoreEntities();

                var orderDetails = _context.OrderDetails
                    .Where(od => od.OrderID == orderId)
                    .Include(od => od.Product)
                    .ToList();

                OrderDetailsDataGrid.ItemsSource = orderDetails;

                var order = _allOrders.FirstOrDefault(o => o.OrderID == orderId);
                if (order != null)
                {
                    SelectedOrderNumber.Text = order.OrderID.ToString();
                    ItemsTotalText.Text = $"{order.TotalAmount:N0} руб.";
                    DeliveryCostText.Text = order.Delivery?.DeliveryCost != null ? $"{order.Delivery.DeliveryCost:N0} руб." : "0 руб.";

                    if (order.DiscountApplied > 0)
                    {
                        decimal discountAmount = order.TotalAmount * order.DiscountApplied / 100;
                        DiscountText.Text = $"-{order.DiscountApplied}% ({discountAmount:N0} руб.)";
                    }
                    else
                    {
                        DiscountText.Text = "0% (0 руб.)";
                    }

                    FinalAmountText.Text = $"{order.FinalAmount:N0} руб.";
                }

                OrderDetailsPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке деталей заказа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите отменить заказ?",
                    "Подтверждение отмены", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context = new AmurStoreEntities();

                        var order = _context.Orders.Find(orderId);
                        if (order != null && (order.OrderStatusID == 1 || order.OrderStatusID == 2))
                        {
                            var canceledStatus = _context.OrderStatus.FirstOrDefault(os => os.OrderStatusName == "Отменен");
                            if (canceledStatus != null)
                            {
                                order.OrderStatusID = canceledStatus.OrderStatusID;
                                _context.SaveChanges();

                                MessageBox.Show("Заказ успешно отменен",
                                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                                LoadOrders();
                                OrderDetailsPanel.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Невозможно отменить заказ в текущем статусе",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при отмене заказа: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void CloseDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            OrderDetailsPanel.Visibility = Visibility.Collapsed;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            _context?.Dispose();
        }
    }
}
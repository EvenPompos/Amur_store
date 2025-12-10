using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity; // ОБЯЗАТЕЛЬНО для работы .Include()
using Amur_store; // Твое пространство имен с классами БД

namespace Amur_store.Views
{
    public partial class OrderPage : Page
    {
        private int clientId;

        public OrderPage(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;

            // Устанавливаем даты по умолчанию (последние 3 месяца)
            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-3);
            EndDatePicker.SelectedDate = DateTime.Now;
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
                    // ИСПРАВЛЕНИЕ: Используем .Include() для быстрой загрузки.
                    // Важно: Имена свойств должны быть как в сгенерированных классах (обычно во множ. числе)
                    var query = db.Orders
                        .Include(o => o.OrdersStatus)
                        .Include(o => o.PaymentsType)
                        .Include(o => o.Deliveries) // Если нужно
                        .Where(o => o.ClientID == clientId);

                    // --- ФИЛЬТРАЦИЯ ---

                    // 1. По статусу
                    if (StatusFilterComboBox.SelectedItem is ComboBoxItem item &&
                        item.Tag != null &&
                        item.Tag.ToString() != "0")
                    {
                        if (int.TryParse(item.Tag.ToString(), out int statusId))
                        {
                            query = query.Where(o => o.OrderStatusID == statusId);
                        }
                    }

                    // 2. По дате
                    if (StartDatePicker.SelectedDate.HasValue)
                    {
                        query = query.Where(o => o.OrderDate >= StartDatePicker.SelectedDate.Value);
                    }

                    if (EndDatePicker.SelectedDate.HasValue)
                    {
                        // Добавляем 1 день, чтобы включить конец выбранной даты
                        var endDate = EndDatePicker.SelectedDate.Value.AddDays(1);
                        query = query.Where(o => o.OrderDate < endDate);
                    }

                    // Выполняем запрос и сортируем
                    var ordersList = query.OrderByDescending(o => o.OrderDate).ToList();

                    OrdersDataGrid.ItemsSource = ordersList;
                    OrdersInfoText.Text = $"Найдено заказов: {ordersList.Count}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}\n\n{ex.InnerException?.Message}");
            }
        }

        // --- Обработчики фильтров ---
        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded) LoadData();
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e) => LoadData();

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            StatusFilterComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-3);
            EndDatePicker.SelectedDate = DateTime.Now;
            LoadData();
        }

        // --- ДЕТАЛИ ЗАКАЗА ---
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
                    // ИСПРАВЛЕНИЕ: Грузим детали вместе с Товарами (Products - мн. число)
                    var details = db.OrderDetails
                        .Include(od => od.Products)
                        .Where(od => od.OrderID == orderId)
                        .ToList();

                    OrderDetailsDataGrid.ItemsSource = details;

                    var order = db.Orders.Find(orderId);
                    if (order != null)
                    {
                        SelectedOrderNumber.Text = order.OrderID.ToString();

                        decimal itemsTotal = details.Sum(d => d.Subtotal ?? 0);

                        // ЛОГИКА ДОСТАВКИ:
                        // В твоем старом коде DeliveryID присваивался как цена. Это ошибка (ID=1 не значит цена 1 рубль).
                        // Ставлю 0. Если у тебя есть цена в таблице Deliveries, нужно писать order.Deliveries.Cost
                        decimal deliveryCost = 0;

                        decimal discountPercent = order.DiscountApplied ?? 0;
                        decimal discountAmount = itemsTotal * (discountPercent / 100);

                        // Если итоговая сумма есть в базе, берем её, иначе считаем
                        decimal final = order.FinalAmount ?? (itemsTotal + deliveryCost - discountAmount);

                        ItemsTotalText.Text = $"{itemsTotal:N0} ₽";
                        DeliveryCostText.Text = $"{deliveryCost:N0} ₽";
                        DiscountText.Text = discountPercent > 0 ? $"-{discountPercent}% ({discountAmount:N0} ₽)" : "0 ₽";
                        FinalAmountText.Text = $"{final:N0} ₽";
                    }

                    OrderDetailsPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей: {ex.Message}");
            }
        }

        private void CloseDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            OrderDetailsPanel.Visibility = Visibility.Collapsed;
        }
    }
}
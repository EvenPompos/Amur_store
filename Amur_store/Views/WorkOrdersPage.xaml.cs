using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity; // Обязательно для .Include()

namespace Amur_store.Views
{
    public partial class WorkOrdersPage : Page
    {
        private Employees _currentUser;

        // Список статусов для выпадающих списков в таблице (Binding)
        public List<OrdersStatus> Statuses { get; set; }

        public WorkOrdersPage(Employees user)
        {
            InitializeComponent();
            _currentUser = user;

            // Устанавливаем DataContext на эту страницу, 
            // чтобы XAML мог видеть список "Statuses"
            DataContext = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // --- ГЛАВНЫЙ МЕТОД ЗАГРУЗКИ ДАННЫХ ---
        private void LoadData()
        {
            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // 1. Загружаем список статусов для ComboBox внутри таблицы
                    Statuses = db.OrdersStatus.ToList();

                    // Трюк для обновления привязки списка статусов в UI (иногда ComboBox не видит список сразу)
                    var temp = DataContext;
                    DataContext = null;
                    DataContext = temp;

                    // 2. Формируем запрос к заказам
                    // Подгружаем Клиентов, Сотрудников и Названия статусов
                    var query = db.Orders
                                  .Include(o => o.Clients)
                                  .Include(o => o.Employees)
                                  .Include(o => o.OrdersStatus)
                                  .AsQueryable();

                    // --- ЛОГИКА ПРАВ ДОСТУПА ---
                    // Если это НЕ Админ (PositionID != 1), показываем только заказы этого сотрудника
                    if (_currentUser.PositionID != 1)
                    {
                        query = query.Where(o => o.EmployeeID == _currentUser.EmployeeID);
                    }

                    // --- ЛОГИКА ФИЛЬТРАЦИИ (Сверху страницы) ---
                    if (FilterStatusBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                    {
                        if (int.TryParse(selectedItem.Tag.ToString(), out int statusFilterId))
                        {
                            // Tag="0" - это "Все заказы", фильтр не нужен
                            if (statusFilterId > 0)
                            {
                                query = query.Where(o => o.OrderStatusID == statusFilterId);
                            }
                        }
                    }

                    // 3. Выполняем запрос и сортируем по дате (сначала новые)
                    var ordersList = query.OrderByDescending(o => o.OrderDate).ToList();

                    // 4. Заполняем таблицу
                    OrdersGrid.ItemsSource = ordersList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка");
            }
        }

        // --- ОБРАБОТЧИКИ СОБЫТИЙ ---

        // 1. Изменение фильтра сверху
        private void FilterStatusBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверка IsLoaded нужна, чтобы не вызывать LoadData при запуске конструктора
            if (this.IsLoaded)
            {
                LoadData();
            }
        }

        // 2. Кнопка "Обновить"
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // 3. Изменение статуса заказа ПРЯМО В ТАБЛИЦЕ
        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            // Получаем объект заказа из строки таблицы
            var currentOrder = comboBox?.DataContext as Orders;

            if (currentOrder != null && comboBox.SelectedValue != null)
            {
                int newStatusId = (int)comboBox.SelectedValue;

                try
                {
                    using (var db = new AmurStoreEntities())
                    {
                        // Находим заказ в БД по ID
                        var dbOrder = db.Orders.Find(currentOrder.OrderID);

                        // Если статус реально изменился
                        if (dbOrder != null && dbOrder.OrderStatusID != newStatusId)
                        {
                            dbOrder.OrderStatusID = newStatusId;
                            db.SaveChanges();

                            // (Опционально) Можно вывести уведомление, но это может раздражать
                            // MessageBox.Show($"Статус заказа №{dbOrder.OrderID} изменен.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось сохранить статус: " + ex.Message);
                    // Если ошибка - перезагружаем таблицу, чтобы вернуть старый статус визуально
                    LoadData();
                }
            }
        }

        // 4. Кнопка "Детали" -> Открыть всплывающее окно
        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null && int.TryParse(button.Tag.ToString(), out int orderId))
            {
                SelectedOrderNumber.Text = orderId.ToString();

                using (var db = new AmurStoreEntities())
                {
                    // Загружаем детали заказа (товары)
                    var details = db.OrderDetails
                                    .Include(od => od.Products)
                                    .Where(od => od.OrderID == orderId)
                                    .ToList();

                    // Загружаем сам заказ для итога и мастера
                    var order = db.Orders
                                  .Include(o => o.Employees)
                                  .FirstOrDefault(o => o.OrderID == orderId);

                    OrderDetailsDataGrid.ItemsSource = details;

                    if (order != null)
                    {
                        DetailTotalText.Text = $"{order.FinalAmount:N0} ₽";
                        DetailMasterText.Text = order.Employees != null
                                                ? $"{order.Employees.Surname} {order.Employees.Name}"
                                                : "Не назначен";
                    }
                }

                // Показываем панель
                OrderDetailsPanel.Visibility = Visibility.Visible;
            }
        }

        // 5. Кнопка "Закрыть" во всплывающем окне
        private void CloseDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            OrderDetailsPanel.Visibility = Visibility.Collapsed;
        }
    }
}
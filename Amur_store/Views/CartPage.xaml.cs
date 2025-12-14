using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Amur_store;

namespace Amur_store.Views
{
    public partial class CartPage : Page
    {
        private int currentClientId;
        private const decimal ASSEMBLY_PRICE = 3000; // Цена сборки константой

        public CartPage()
        {
            InitializeComponent();
            this.currentClientId = 1;
        }

        public CartPage(int clientId)
        {
            InitializeComponent();
            this.currentClientId = clientId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMasters(); // Загружаем список мастеров
            UpdateCartDisplay();
        }

        // --- НОВЫЙ МЕТОД: Загрузка мастеров ---
        private void LoadMasters()
        {
            try
            {
                using (var db = new AmurStoreEntities())
                {
                    // Выбираем сотрудников с PositionID = 3 (Сборщик)
                    // Формируем анонимный объект или используем класс, чтобы отобразить ФИО
                    var masters = db.Employees
                        .Where(e => e.PositionID == 3)
                        .Select(e => new
                        {
                            e.EmployeeID,
                            FullName = e.Surname + " " + e.Name + " " + e.Patronymic
                        })
                        .ToList();

                    MastersComboBox.ItemsSource = masters;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить список мастеров: " + ex.Message);
            }
        }

        private void UpdateCartDisplay()
        {
            try
            {
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

        // --- ОБНОВЛЕННЫЙ ПОДСЧЕТ СУММ ---
        private void CalculateTotals(List<BasketItem> items)
        {
            decimal subtotal = items.Sum(item => item.Total);
            decimal deliveryCost = GetDeliveryCost();

            // Если галочка стоит, добавляем 3000 к итоговой сумме визуально
            decimal assemblyCost = (AssemblyCheckBox.IsChecked == true) ? ASSEMBLY_PRICE : 0;

            decimal total = subtotal + deliveryCost + assemblyCost;

            SubtotalText.Text = $"{subtotal:N0} ₽";
            DeliveryCostText.Text = $"{deliveryCost:N0} ₽";

            // Можно добавить отображение стоимости сборки, если хочешь, но она уже включена в Total
            TotalText.Text = $"{total:N0} ₽";
        }

        // --- НОВОЕ СОБЫТИЕ: Переключение галочки сборки ---
        private void AssemblyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (AssemblyCheckBox.IsChecked == true)
            {
                MasterSelectionPanel.Visibility = Visibility.Visible; // Показываем список
            }
            else
            {
                MasterSelectionPanel.Visibility = Visibility.Collapsed; // Скрываем
                MastersComboBox.SelectedItem = null; // Сбрасываем выбор
            }

            // Пересчитываем сумму (чтобы добавить/убрать 3000)
            if (GlobalBasket.Items.Count > 0)
            {
                CalculateTotals(GlobalBasket.Items.Values.ToList());
            }
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

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out int productId))
            {
                if (GlobalBasket.Items.ContainsKey(productId))
                {
                    GlobalBasket.Items.Remove(productId);
                    UpdateCartDisplay();
                }
            }
        }

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
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        // Метод определяет ID склада, проходя по цепочке Product -> Manufacture -> Category
        private int GetWarehouseForProduct(AmurStoreEntities db, int productId)
        {
            // 1. Находим товар
            var product = db.Products.FirstOrDefault(p => p.ProductID == productId);
            if (product == null) return 4; // Если товар не найден, по умолчанию склад деталей

            // 2. Находим производителя по ManufacturyID
            var manufacture = db.Manufactures.FirstOrDefault(m => m.ManufacturyID == product.ManufacturyID);

            // Если производителя нет, или CategoryID null - отправляем на склад деталей (4)
            if (manufacture == null) return 4;

            int catId = manufacture.CategoryID;

            // 3. Проверяем ID категорий готовой продукции
            // 1=ПК, 2=Моноблоки, 3=Неттопы, 4=Сервера, 14=Ноутбуки
            if (catId == 1 || catId == 2 || catId == 3 || catId == 4 || catId == 14)
            {
                return 2; // Склад готовой продукции "Быково"
            }

            // Все остальное (Материнки, ЦП, ОЗУ и т.д.) - Склад комплектующих "Красный холм"
            return 4;
        }

        // --- ОБНОВЛЕННОЕ ОФОРМЛЕНИЕ ЗАКАЗА ---
        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalBasket.Items.Count == 0) return;

            // Проверка: выбрана сборка, но не выбран мастер
            if (AssemblyCheckBox.IsChecked == true && MastersComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите мастера для сборки ПК!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new AmurStoreEntities())
                {
                    var cartItems = GlobalBasket.Items.Values.ToList();

                    // -----------------------------------------------------------
                    // 1. ПРОВЕРКА ОСТАТКОВ (С учетом категорий и складов)
                    // -----------------------------------------------------------
                    foreach (var item in cartItems)
                    {
                        // Получаем товар для проверки имени (чтобы пропустить услуги)
                        var productInDb = db.Products.FirstOrDefault(p => p.ProductID == item.ProductID);

                        // Если это "Услуга" или "Сборка", остатки не проверяем
                        if (productInDb != null && (productInDb.ProductName.Contains("Услуга") || productInDb.ProductName.Contains("Сборка")))
                            continue;

                        // ОПРЕДЕЛЯЕМ НУЖНЫЙ СКЛАД ПО ЦЕПОЧКЕ Products -> Manufactures
                        int targetWarehouseID = GetWarehouseForProduct(db, item.ProductID);

                        // Ищем запись в Stock
                        var stockItem = db.Stock.FirstOrDefault(s => s.ProductID == item.ProductID && s.WarehouseID == targetWarehouseID);

                        // Если записи нет или мало товара
                        if (stockItem == null || stockItem.Quantity < item.Quantity)
                        {
                            string warehouseName = (targetWarehouseID == 2) ? "Склад готовой продукции" : "Склад комплектующих";
                            MessageBox.Show($"Товара '{item.ProductName}' недостаточно!\nТребуется склад: {warehouseName} (ID {targetWarehouseID})", "Ошибка наличия");
                            return; // Отменяем всё
                        }
                    }

                    // -----------------------------------------------------------
                    // 2. СОЗДАНИЕ ЗАКАЗА
                    // -----------------------------------------------------------

                    // Если есть сборка -> основной склад заказа 4 (мастерская), иначе 2 (магазин/склад готового)
                    int mainWarehouseId = (AssemblyCheckBox.IsChecked == true) ? 4 : 2;

                    decimal productsTotal = cartItems.Sum(i => i.Total);
                    decimal deliveryCost = GetDeliveryCost();
                    decimal assemblyCost = (AssemblyCheckBox.IsChecked == true) ? ASSEMBLY_PRICE : 0;
                    int? selectedMasterId = (AssemblyCheckBox.IsChecked == true) ? (int?)MastersComboBox.SelectedValue : null;

                    var newOrder = new Orders
                    {
                        ClientID = currentClientId,
                        OrderDate = DateTime.Now,
                        DeliveryID = DeliveryComboBox.SelectedIndex + 1,
                        WarehouseID = mainWarehouseId, // Основной склад логистики
                        OrderStatusID = 1,
                        PaymentTypeID = 1,
                        PaymentStatusID = 1,
                        EmployeeID = selectedMasterId,
                        TotalAmount = productsTotal + assemblyCost,
                        DiscountApplied = 0,
                        FinalAmount = productsTotal + assemblyCost + deliveryCost
                    };

                    db.Orders.Add(newOrder);
                    db.SaveChanges(); // Получаем ID заказа

                    // -----------------------------------------------------------
                    // 3. ДОБАВЛЕНИЕ ДЕТАЛЕЙ И СПИСАНИЕ
                    // -----------------------------------------------------------
                    foreach (var item in cartItems)
                    {
                        // Добавляем в OrderDetails
                        db.OrderDetails.Add(new OrderDetails
                        {
                            OrderID = newOrder.OrderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price,
                            Subtotal = item.Total
                        });

                        // Списываем со склада
                        var productInDb = db.Products.FirstOrDefault(p => p.ProductID == item.ProductID);

                        // Если это физический товар (не услуга)
                        if (productInDb != null && !productInDb.ProductName.Contains("Услуга") && !productInDb.ProductName.Contains("Сборка"))
                        {
                            // Снова вычисляем склад, чтобы списать именно оттуда
                            int targetWarehouseID = GetWarehouseForProduct(db, item.ProductID);

                            var stockToUpdate = db.Stock.FirstOrDefault(s => s.ProductID == item.ProductID && s.WarehouseID == targetWarehouseID);
                            if (stockToUpdate != null)
                            {
                                stockToUpdate.Quantity -= item.Quantity;
                                stockToUpdate.LastUpdated = DateTime.Now;
                            }
                        }
                    }

                    // 4. Добавляем услугу сборки (если галочка стоит)
                    if (AssemblyCheckBox.IsChecked == true)
                    {
                        // Ищем товар-услугу в базе
                        var assemblyService = db.Products.FirstOrDefault(p => p.ProductName.Contains("Сборка"));

                        // Если вдруг не нашли по имени, пробуем найти по ID (у тебя вроде был 81, но лучше по имени)
                        if (assemblyService == null) assemblyService = db.Products.FirstOrDefault(p => p.ProductID == 81);

                        if (assemblyService != null)
                        {
                            db.OrderDetails.Add(new OrderDetails
                            {
                                OrderID = newOrder.OrderID,
                                ProductID = assemblyService.ProductID,
                                Quantity = 1,
                                UnitPrice = ASSEMBLY_PRICE,
                                Subtotal = ASSEMBLY_PRICE
                            });
                        }
                    }

                    db.SaveChanges(); // Финальное сохранение изменений в Stock и OrderDetails

                    MessageBox.Show($"Заказ №{newOrder.OrderID} успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    GlobalBasket.Items.Clear();
                    UpdateCartDisplay();
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Ошибка БД:\n{msg}", "Ошибка");
            }
        }
    }
}
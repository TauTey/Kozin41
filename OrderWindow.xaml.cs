using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kozin41
{
    /// <summary>
    /// Логика взаимодействия для OrderWindow.xaml
    /// </summary>
    public partial class OrderWindow : Page
    {
        private Order _currentOrder;
        public OrderWindow()
        {
            InitializeComponent();
            Loaded += OrderWindow_Loaded;
        }

        private void OrderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOrderData();
        }

        private void LoadOrderData()
        {
            try
            {
                using (var context = Kozin41Entities.GetContext())
                {
                    // Находим текущий заказ
                    _currentOrder = context.Order
                        .Include("OrderProduct")
                        .Include("OrderProduct.Product")
                        .Include("PickUpPoint")
                        .Include("User")
                        .FirstOrDefault(o => o.OrderStatus == "Новый" &&
                                            o.OrderClientID == Manager.CurrentUser.UserID);

                    if (_currentOrder == null)
                    {
                        MessageBox.Show("Нет активного заказа");
                        Close();
                        return;
                    }

                    // Заполняем данные
                    StartDP.SelectedDate = _currentOrder.OrderDate;
                    OrderDP.SelectedDate = _currentOrder.OrderDeliveryDate;
                    TBOrderID.Text = _currentOrder.OrderCode.ToString();

                    // ФИО клиента
                    if (_currentOrder.User != null)
                    {
                        ClientTB.Text = $"{_currentOrder.User.UserSurname} {_currentOrder.User.UserName} {_currentOrder.User.UserPatronymic}";
                    }
                    else
                    {
                        ClientTB.Text = "Гость";
                    }

                    // Пункты выдачи
                    var points = context.PickUpPoint.ToList();
                    PickupCombo.ItemsSource = points;
                    if (_currentOrder.PickUpPoint != null)
                    {
                        PickupCombo.SelectedValue = _currentOrder.PickUpPoint.PickUpPointID;
                    }

                    // Товары
                    OrderItemsListView.ItemsSource = _currentOrder.OrderProduct.ToList();

                    // Рассчитываем итоги
                    CalculateTotal();

                    // Расчет срока доставки
                    CalculateDeliveryDate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void CalculateTotal()
        {
            if (!_currentOrder.OrderProduct.Any())
            {
                TotalText.Text = "Итого: 0 руб.";
                return;
            }

            decimal total = 0;
            decimal discount = 0;

            foreach (var item in _currentOrder.OrderProduct)
            {
                decimal price = item.Product.ProductCost;
                decimal itemDiscount = price * (item.Product.ProductDiscountAmount / 100m);
                total += price * item.ProductQuantity;
                discount += itemDiscount * item.ProductQuantity;
            }

            TotalText.Text = $"Итого: {total - discount:C} (скидка: {discount:C})";
        }

        private void CalculateDeliveryDate()
        {
            if (!_currentOrder.OrderProduct.Any()) return;

            // Проверяем наличие товаров
            bool allInStock = _currentOrder.OrderProduct.All(op =>
                op.Product.ProductQuantityInStock >= op.ProductQuantity);

            int availablePositions = _currentOrder.OrderProduct.Count(op =>
                op.Product.ProductQuantityInStock >= 3);

            int deliveryDays = (allInStock && availablePositions >= 3) ? 3 : 6;

            // Обновляем дату доставки
            OrderDP.SelectedDate = DateTime.Now.AddDays(deliveryDays);
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string article)
            {
                try
                {
                    using (var context = Kozin41Entities.GetContext())
                    {
                        var item = context.OrderProduct
                            .FirstOrDefault(op => op.OrderID == _currentOrder.OrderID &&
                                                 op.ProductArticleNumber == article);

                        if (item != null)
                        {
                            context.OrderProduct.Remove(item);
                            context.SaveChanges();

                            // Обновляем список
                            LoadOrderData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}");
                }
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = Kozin41Entities.GetContext())
                {
                    var order = context.Order.Find(_currentOrder.OrderID);
                    if (order != null)
                    {
                        // Обновляем пункт выдачи
                        if (PickupCombo.SelectedItem is PickUpPoint selectedPoint)
                        {
                            order.OrderPickupPoint = selectedPoint.PickUpPointID;
                        }

                        // Обновляем дату доставки
                        if (OrderDP.SelectedDate.HasValue)
                        {
                            order.OrderDeliveryDate = OrderDP.SelectedDate.Value;
                        }

                        // Меняем статус
                        order.OrderStatus = "Подтвержден";

                        context.SaveChanges();

                        MessageBox.Show($"Заказ №{order.OrderCode} сохранен!");
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }
    }
}

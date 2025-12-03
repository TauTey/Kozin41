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
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        public ProductPage(User user)
        {
            InitializeComponent();
            if (user != null)
            {
                FIOTB.Text = "Вы авторизовались как: " + user.UserSurname + " " + user.UserName + " " + user.UserPatronymic;
                switch (user.UserRole)
                {
                    case 1:
                        RoleTB.Text = "Роль: Клиент";
                        break;
                    case 2:
                        RoleTB.Text = "Роль: Менеджер";
                        break;
                    case 3:
                        RoleTB.Text = "Роль: Администратор";
                        break;
                }
            }
            else
            {
                FIOTB.Text = "Вы авторизовались как: Гость";
                RoleTB.Text = "";
            }
            var currentProducts = Kozin41Entities.GetContext().Product.ToList();
            ProductListView.ItemsSource = currentProducts;

            ComboType.SelectedIndex = 0;
            TextItemCount.Text = $"{currentProducts.Count} из {Kozin41Entities.GetContext().Product.Count()}";

            Manager.CurrentUser = user;
        }

        private void UpdateProducts()
        {
            var currentProducts = Kozin41Entities.GetContext().Product.ToList();
            if (ComboType.SelectedIndex == 0)
            {
                currentProducts = currentProducts.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 0 && Convert.ToInt32(p.ProductDiscountAmount) <= 100)).ToList();
            }
            if (ComboType.SelectedIndex == 1)
            {
                currentProducts = currentProducts.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 0 && Convert.ToInt32(p.ProductDiscountAmount) < 9.99)).ToList();
            }
            if (ComboType.SelectedIndex == 2)
            {
                currentProducts = currentProducts.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 10 && Convert.ToInt32(p.ProductDiscountAmount) <= 14.99)).ToList();
            }
            if (ComboType.SelectedIndex == 3)
            {
                currentProducts = currentProducts.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 15 && Convert.ToInt32(p.ProductDiscountAmount) <= 100)).ToList();
            }
            currentProducts = currentProducts.Where(p => p.ProductName.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList();

            if (RButtonDown.IsChecked == true)
            {
                currentProducts = currentProducts.OrderByDescending(p => p.ProductCost).ToList();
            }
            if (RButtonUp.IsChecked == true)
            {
                currentProducts = currentProducts.OrderBy(p => p.ProductCost).ToList();
            }
            ProductListView.ItemsSource = currentProducts;
            TextItemCount.Text = $"{currentProducts.Count} из {Kozin41Entities.GetContext().Product.Count()}";
        }
        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProducts();
        }
        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProducts();
        }

        private void RButtonUp_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProducts();
        }

        private void RButtonDown_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProducts();
        }

        private void AddToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ProductListView.SelectedItem is Product selectedProduct)
            {
                try
                {
                    using (var context = Kozin41Entities.GetContext())
                    {
                        // Находим или создаем заказ
                        var order = context.Order
                            .FirstOrDefault(o => o.OrderStatus == "Новый" && o.OrderClientID == Manager.CurrentUser.UserID);

                        if (order == null)
                        {
                            // Создаем новый заказ
                            order = new Order
                            {
                                OrderDate = DateTime.Now.Date,
                                OrderDeliveryDate = DateTime.Now.Date,
                                OrderPickupPoint = 1,
                                OrderClientID = Manager.CurrentUser?.UserID,
                                OrderCode = (context.Order.Max(o => (int?)o.OrderCode) ?? 900) + 1,
                                OrderStatus = "Новый"
                            };
                            context.Order.Add(order);
                            context.SaveChanges();
                        }

                        // Добавляем товар
                        var existingItem = context.OrderProduct
                            .FirstOrDefault(op => op.OrderID == order.OrderID &&
                                                 op.ProductArticleNumber == selectedProduct.ProductArticleNumber);

                        if (existingItem != null)
                        {
                            existingItem.ProductQuantity += 1;
                        }
                        else
                        {
                            var newItem = new OrderProduct
                            {
                                OrderID = order.OrderID,
                                ProductArticleNumber = selectedProduct.ProductArticleNumber,
                                ProductQuantity = 1
                            };
                            context.OrderProduct.Add(newItem);
                        }

                        context.SaveChanges();
                        MessageBox.Show("Товар добавлен к заказу!");

                        // Можно показать кнопку просмотра заказа
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }
    }
}

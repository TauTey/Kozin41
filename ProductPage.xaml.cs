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
        int CountRecords;
        int CountPage;
        int CurrentPage = 0;
        List<Product> CurrentPageList = new List<Product>();
        List<Product> TableList;
        List<OrderProduct> selectedOrderProducts = new List<OrderProduct>();
        List<Product> selectedProducts = new List<Product>();
        public ProductPage(User user)
        {
            var CurrentProduct = Kozin41Entities.GetContext().Product.ToList();
            InitializeComponent();
            if (user != null)
            {
                FIOTB.Text = user.UserSurname + " " + user.UserName + " " + user.UserPatronymic;
                switch (user.UserRole)
                {
                    case 1:
                        RoleTB.Text = "Клиент"; break;
                    case 2:
                        RoleTB.Text = "Менеджер"; break;
                    case 3:
                        RoleTB.Text = "Администратор"; break;
                }
            }
            else
            {
                FIOTB.Text = "Гость";
                RoleTB.Text = "Гость";
            }


            ProductListView.ItemsSource = CurrentProduct;
            ComboType.SelectedIndex = 0;

            UpdateProduct();
        }
        private void UpdateProduct()
        {
            var CurrentProduct = Kozin41Entities.GetContext().Product.ToList();
            CurrentPageList = Kozin41Entities.GetContext().Product.ToList();
            if (ComboType.SelectedIndex == 0)
                CurrentProduct = CurrentProduct.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 0 && Convert.ToInt32(p.ProductDiscountAmount) <= 100)).ToList();
            if (ComboType.SelectedIndex == 1)
                CurrentProduct = CurrentProduct.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 0 && Convert.ToInt32(p.ProductDiscountAmount) < 10)).ToList();
            if (ComboType.SelectedIndex == 2)
                CurrentProduct = CurrentProduct.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 10 && Convert.ToInt32(p.ProductDiscountAmount) < 15)).ToList();
            if (ComboType.SelectedIndex == 3)
                CurrentProduct = CurrentProduct.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 15)).ToList();
            CurrentProduct = CurrentProduct.Where(p => p.ProductName.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList();
            ProductListView.ItemsSource = CurrentProduct.ToList();
            if (RButtonDown.IsChecked.Value)
                CurrentProduct = CurrentProduct.OrderByDescending(p => p.ProductCost).ToList();
            if (RButtonUp.IsChecked.Value)
                CurrentProduct = CurrentProduct.OrderBy(p => p.ProductCost).ToList();
            ProductListView.ItemsSource = CurrentProduct;
            TableList = CurrentProduct;
            ChangePage(0);
        }
        private void ChangePage(int? selectedPage)
        {


            TBCount.Text = "0";
            CountRecords = TableList.Count;
            TBCount.Text = CountRecords.ToString();
            TBAllRecords.Text = " из " + CurrentPageList.Count;
            if (selectedProducts.Count <= 0)
            {
                OrdersBtn.Visibility = Visibility.Hidden;
            }
            else
            {
                OrdersBtn.Visibility = Visibility.Visible;
            }
        }


        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProduct();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProduct();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProduct();
        }

        private void RButtonUp_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProduct();
        }

        private void RButtonDown_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProduct();

        }




        private void UpdateOrdersButtonVisibility()
        {
            // Добавьте отладку
            Console.WriteLine($"UpdateOrdersButtonVisibility called. selectedProducts.Count = {selectedProducts.Count}");

            // Кнопка OrdersBtn видима, если есть товары в корзине
            if (OrdersBtn != null)
            {
                OrdersBtn.Visibility = selectedProducts.Count > 0 ? Visibility.Visible : Visibility.Hidden;
                Console.WriteLine($"OrdersBtn.Visibility set to: {OrdersBtn.Visibility}");
            }
            else
            {
                Console.WriteLine("ERROR: OrdersBtn is null!");
            }
        }

        private void OrdersBtn_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("OrdersBtn clicked!");

            if (selectedProducts.Count > 0)
            {
                Console.WriteLine($"selectedProducts.Count = {selectedProducts.Count}");

                selectedProducts = selectedProducts.Distinct().ToList();
                OrderWindow orderWindow = new OrderWindow(selectedOrderProducts, selectedProducts, FIOTB.Text);

                // Скрываем кнопку при открытии корзины
                OrdersBtn.Visibility = Visibility.Hidden;
                Console.WriteLine("Button hidden before opening OrderWindow");

                orderWindow.ShowDialog();
                Console.WriteLine("OrderWindow closed");

                // Очищаем корзину после закрытия окна
                selectedProducts.Clear();
                selectedOrderProducts.Clear();
                Console.WriteLine("Cart cleared");
            }
            else
            {
                Console.WriteLine("Cart is empty");
                MessageBox.Show("Корзина пуста");
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("MenuItem clicked (Add to cart)");

            if (ProductListView.SelectedIndex >= 0)
            {
                var Prod = ProductListView.SelectedItem as Product;
                Console.WriteLine($"Adding product: {Prod?.ProductName}");
                selectedProducts.Add(Prod);

                var newOrderProd = new OrderProduct
                {
                    OrderID = selectedOrderProducts.Count() + 1,
                    ProductArticleNumber = Prod.ProductArticleNumber,
                    ProductQuantity = 1
                };

                var selOP = selectedOrderProducts.Where(p => Equals(p.ProductArticleNumber, Prod.ProductArticleNumber));

                if (selOP.Count() == 0)
                {
                    selectedOrderProducts.Add(newOrderProd);
                    Console.WriteLine("New product added to order");
                }
                else
                {
                    foreach (OrderProduct p in selectedOrderProducts)
                    {
                        if (p.ProductArticleNumber == Prod.ProductArticleNumber)
                            p.ProductQuantity++;
                    }
                    Console.WriteLine("Product quantity increased");
                }

                // Показываем кнопку OrdersBtn при добавлении товара
                Console.WriteLine($"selectedProducts.Count now = {selectedProducts.Count}");
                OrdersBtn.Visibility = Visibility.Visible;
                Console.WriteLine("Button set to Visible");
                ProductListView.SelectedIndex = -1;
            }
            else
            {
                Console.WriteLine("No product selected");
            }
        }

    }
}

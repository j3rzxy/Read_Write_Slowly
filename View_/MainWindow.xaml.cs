using Read_Write_Slowly.Models_;
using Read_Write_Slowly.ViewModels_;
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

namespace Read_Write_Slowly
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Конструктор по умолчанию (нужен для дизайнера Visual Studio)
        public MainWindow()
        {
            InitializeComponent();
        }

        // Основной конструктор, вызываемый при успешном входе
        public MainWindow(User authenticatedUser)
        {
            InitializeComponent();

            // Привязываем DataContext к MainViewModel, передавая пользователя
            this.DataContext = new MainViewModel(authenticatedUser);
        }
    }
}

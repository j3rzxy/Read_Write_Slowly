using System.Windows;

namespace Read_Write_Slowly.View_
{
    public partial class BookReaderWindow : Window
    {
        public BookReaderWindow(string title, string authorName, string content)
        {
            InitializeComponent();
            DataContext = new ViewModels_.BookReaderViewModel(title, authorName, content);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

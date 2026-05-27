using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class BookReaderViewModel : BaseViewModel
    {
        private const int FontMin = 12;
        private const int FontMax = 28;

        public string BookTitle   { get; }
        public string AuthorName  { get; }
        public string ContentText { get; }
        public string WindowTitle => $"Читалка — {BookTitle}";
        public int    CharCount   => ContentText?.Length ?? 0;

        private int _fontSize = 16;
        public int FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(); }
        }

        public ICommand IncreaseFontCommand { get; }
        public ICommand DecreaseFontCommand { get; }

        public BookReaderViewModel(string title, string authorName, string content)
        {
            BookTitle   = title      ?? "Без названия";
            AuthorName  = authorName ?? "";
            ContentText = string.IsNullOrWhiteSpace(content)
                          ? "Текст книги ещё не добавлен автором."
                          : content;

            IncreaseFontCommand = new RelayCommand(
                () => { if (FontSize < FontMax) FontSize++; });

            DecreaseFontCommand = new RelayCommand(
                () => { if (FontSize > FontMin) FontSize--; });
        }
    }
}

using Read_Write_Slowly.Repositories_;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class CatalogViewModel : BaseViewModel // Предполагается реализация INotifyPropertyChanged
    {
        private readonly BookRepository _bookRepository;

        // Свойства для фильтрации и поиска
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); LoadBooks(); } // Поиск при вводе текста
        }

        private Genre _selectedGenre;
        public Genre SelectedGenre
        {
            get => _selectedGenre;
            set { _selectedGenre = value; OnPropertyChanged(); LoadBooks(); }
        }

        private string _selectedSort = "Name"; // По умолчанию по имени
        public string SelectedSort
        {
            get => _selectedSort;
            set { _selectedSort = value; OnPropertyChanged(); LoadBooks(); }
        }

        // Коллекции для привязки к UI
        public ObservableCollection<Book> Books { get; set; }
        public List<Genre> Genres { get; set; }

        // Команды
        public ICommand OpenBookCommand { get; }
        public ICommand AddToTrackListCommand { get; }

        public CatalogViewModel()
        {
            _bookRepository = new BookRepository();
            Books = new ObservableCollection<Book>();

            // Загружаем жанры
            Genres = _bookRepository.GetGenres();
            Genres.Insert(0, new Genre { GenreId = 0, Name = "Все жанры" }); // Элемент сброса фильтра

            // Инициализация команд
            OpenBookCommand = new RelayCommand<Book>(OpenBookDetails);
            AddToTrackListCommand = new RelayCommand<object>(AddBookToSelectedList);

            LoadBooks();
        }

        private void LoadBooks()
        {
            int? genreId = (SelectedGenre == null || SelectedGenre.GenreId == 0) ? null : (int?)SelectedGenre.GenreId;
            var filtered = _bookRepository.GetFilteredBooks(SearchText, genreId, SelectedSort);

            Books.Clear();
            foreach (var book in filtered)
            {
                Books.Add(book);
            }
        }

        private void OpenBookDetails(Book book)
        {
            if (book == null) return;
            // Логика навигации на страницу конкретной книги (BookPage)
            // Например: MainNavigation.NavigateTo(new BookViewModel(book.BookId));
        }

        private void AddBookToSelectedList(object parameter)
        {
            // Параметр передает массив или структуру: книга и выбранный статус списка
            // Пример упрощенной логики:
            var values = parameter as object[];
            if (values != null && values.Length == 2)
            {
                var book = values[0] as Book;
                string status = values[1].ToString(); // "Читаю", "В планах" и т.д.

                // App.CurrentUser — статический класс/свойство, хранящее текущего залогиненного юзера
                int currentUserId = 1; // Заглушка, тут должен быть Id авторизованного пользователя

                _bookRepository.AddBookToReadingList(currentUserId, book.BookId, status);
                System.Windows.MessageBox.Show($"Книга добавлена в список '{status}'");
            }
        }
    }
}

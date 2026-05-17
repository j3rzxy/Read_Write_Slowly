using Read_Write_Slowly.Repositories_;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class UserListsViewModel : BaseViewModel
    {
        private readonly ReadingListRepository _listRepository;
        private readonly BookRepository _bookRepository; // Используем методы жанров из прошлого репозитория
        private readonly int _currentUserId = 1; // Заглушка (в реальности берется из сессии)

        // Текущий выбранный список (вкладка)
        private string _selectedTabStatus = "В планах";
        public string SelectedTabStatus
        {
            get => _selectedTabStatus;
            set { _selectedTabStatus = value; OnPropertyChanged(); LoadCurrentList(); }
        }

        // Поиск и фильтрация
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); LoadCurrentList(); }
        }

        private Genre _selectedGenre;
        public Genre SelectedGenre
        {
            get => _selectedGenre;
            set { _selectedGenre = value; OnPropertyChanged(); LoadCurrentList(); }
        }

        private string _selectedSort = "Name";
        public string SelectedSort
        {
            get => _selectedSort;
            set { _selectedSort = value; OnPropertyChanged(); LoadCurrentList(); }
        }

        // Коллекции
        public ObservableCollection<Book> DisplayedBooks { get; set; }
        public List<Genre> Genres { get; set; }

        // Команды
        public ICommand MoveBookCommand { get; }
        public ICommand OpenBookCommand { get; }

        public UserListsViewModel()
        {
            _listRepository = new ReadingListRepository();
            _bookRepository = new BookRepository();
            DisplayedBooks = new ObservableCollection<Book>();

            // Загрузка жанров для комбобокса
            Genres = _bookRepository.GetGenres();
            Genres.Insert(0, new Genre { GenreId = 0, Name = "Все жанры" });

            // Инициализация команд
            MoveBookCommand = new RelayCommand<object>(MoveBook);
            OpenBookCommand = new RelayCommand<Book>(OpenBookDetails);

            LoadCurrentList();
        }

        // Загрузка книг для текущего выбранного статуса
        private void LoadCurrentList()
        {
            int? genreId = (SelectedGenre == null || SelectedGenre.GenreId == 0) ? null : (int?)SelectedGenre.GenreId;

            var books = _listRepository.GetUserReadingList(_currentUserId, SelectedTabStatus, SearchText, genreId, SelectedSort);

            DisplayedBooks.Clear();
            foreach (var book in books)
            {
                DisplayedBooks.Add(book);
            }
        }

        // Перемещение книги в другой список
        private void MoveBook(object parameter)
        {
            var values = parameter as object[];
            if (values != null && values.Length == 2)
            {
                var book = values[0] as Book;
                string newStatus = values[1].ToString();

                _listRepository.MoveBookToAnotherList(_currentUserId, book.BookId, newStatus);

                // После перемещения удаляем книгу из текущего отображения на UI
                DisplayedBooks.Remove(book);
            }
        }

        private void OpenBookDetails(Book book)
        {
            if (book == null) return;
            // Переход на страницу деталей книги (BookPage)
        }
    }
}

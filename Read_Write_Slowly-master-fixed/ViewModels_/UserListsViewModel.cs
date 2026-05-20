using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class UserListsViewModel : BaseViewModel
    {
        private readonly ReadingListRepository _listRepository;
        private readonly BookRepository _bookRepository;
        private readonly int _currentUserId;
        private readonly Action<object> _navigateTo;

        // Текущая вкладка
        private string _selectedTabStatus = "В планах";
        public string SelectedTabStatus
        {
            get => _selectedTabStatus;
            set { _selectedTabStatus = value; OnPropertyChanged(); LoadCurrentList(); }
        }

        // Фильтрация
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

        // Принимает реальный userId и делегат навигации
        public UserListsViewModel(int userId, Action<object> navigateTo)
        {
            _currentUserId = userId;
            _navigateTo = navigateTo;
            _listRepository = new ReadingListRepository();
            _bookRepository = new BookRepository();
            DisplayedBooks = new ObservableCollection<Book>();

            Genres = _bookRepository.GetGenres();
            Genres.Insert(0, new Genre { GenreId = 0, Name = "Все жанры" });

            MoveBookCommand = new RelayCommand<object>(MoveBook);
            OpenBookCommand = new RelayCommand<Book>(OpenBookDetails);

            LoadCurrentList();
        }

        private void LoadCurrentList()
        {
            int? genreId = (SelectedGenre == null || SelectedGenre.GenreId == 0)
                           ? (int?)null
                           : SelectedGenre.GenreId;

            var books = _listRepository.GetUserReadingList(
                _currentUserId, SelectedTabStatus, SearchText, genreId, SelectedSort);

            DisplayedBooks.Clear();
            foreach (var book in books)
                DisplayedBooks.Add(book);
        }

        private void MoveBook(object parameter)
        {
            var values = parameter as object[];
            if (values == null || values.Length != 2) return;

            var book = values[0] as Book;
            string status = values[1]?.ToString();
            if (book == null || status == null) return;

            _listRepository.MoveBookToAnotherList(_currentUserId, book.BookId, status);
            DisplayedBooks.Remove(book);
        }

        private void OpenBookDetails(Book book)
        {
            if (book == null || _navigateTo == null) return;

            // Получаем текущего пользователя из репозитория — или передаём дальше из конструктора
            // Здесь используем заглушку-сервис; в реальности лучше передавать User через конструктор
            var userRepo = new UserRepository();
            var user = userRepo.GetUserById(_currentUserId);

            var bookVm = new BookDetailsViewModel(
                book.BookId,
                user,
                () => _navigateTo(new UserListsViewModel(_currentUserId, _navigateTo)));

            _navigateTo(bookVm);
        }
    }
}
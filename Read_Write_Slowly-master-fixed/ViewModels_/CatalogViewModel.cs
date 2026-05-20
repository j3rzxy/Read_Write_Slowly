using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class CatalogViewModel : BaseViewModel
    {
        private readonly BookRepository _bookRepository;
        private readonly User _currentUser;
        private readonly Action<object> _navigateTo;

        // Фильтрация и поиск
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); LoadBooks(); }
        }

        private Genre _selectedGenre;
        public Genre SelectedGenre
        {
            get => _selectedGenre;
            set { _selectedGenre = value; OnPropertyChanged(); LoadBooks(); }
        }

        private string _selectedSort = "Name";
        public string SelectedSort
        {
            get => _selectedSort;
            set { _selectedSort = value; OnPropertyChanged(); LoadBooks(); }
        }

        // Коллекции для UI
        public ObservableCollection<Book> Books { get; set; }
        public List<Genre> Genres { get; set; }

        // Команды
        public ICommand OpenBookCommand { get; }
        public ICommand AddToTrackListCommand { get; }

        // Принимает текущего пользователя и делегат навигации от MainViewModel
        public CatalogViewModel(User currentUser, Action<object> navigateTo)
        {
            _currentUser = currentUser;
            _navigateTo = navigateTo;
            _bookRepository = new BookRepository();
            Books = new ObservableCollection<Book>();

            Genres = _bookRepository.GetGenres();
            Genres.Insert(0, new Genre { GenreId = 0, Name = "Все жанры" });

            OpenBookCommand = new RelayCommand<Book>(OpenBookDetails);
            AddToTrackListCommand = new RelayCommand<object>(AddBookToSelectedList);

            LoadBooks();
        }

        private void LoadBooks()
        {
            int? genreId = (SelectedGenre == null || SelectedGenre.GenreId == 0)
                           ? (int?)null
                           : SelectedGenre.GenreId;

            var filtered = _bookRepository.GetFilteredBooks(SearchText, genreId, SelectedSort);
            Books.Clear();
            foreach (var book in filtered)
                Books.Add(book);
        }

        private void OpenBookDetails(Book book)
        {
            if (book == null) return;

            // Переходим на страницу книги, передаём делегат «назад» — вернуться в каталог
            var bookVm = new BookDetailsViewModel(
                book.BookId,
                _currentUser,
                () => _navigateTo(new CatalogViewModel(_currentUser, _navigateTo)));

            _navigateTo(bookVm);
        }

        private void AddBookToSelectedList(object parameter)
        {
            var values = parameter as object[];
            if (values == null || values.Length != 2) return;

            var book = values[0] as Book;
            string status = values[1]?.ToString();
            if (book == null || status == null) return;

            _bookRepository.AddBookToReadingList(_currentUser.UserId, book.BookId, status);
            System.Windows.MessageBox.Show($"Книга добавлена в список «{status}»", "Готово");
        }
    }
}
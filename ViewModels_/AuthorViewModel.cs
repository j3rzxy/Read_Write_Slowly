using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

// Алиасы для разрешения конфликта между EF-классами и Models_-классами
using BookModel = Read_Write_Slowly.Models_.Book;
using SelectableGenreModel = Read_Write_Slowly.Models_.SelectableGenre;

namespace Read_Write_Slowly.ViewModels_
{
    public class AuthorViewModel : BaseViewModel
    {
        private readonly AuthorRepository _repository;
        private readonly int _authorUserId;

        public ICommand BrowseCoverCommand { get; }

        public ObservableCollection<BookModel> MyBooks { get; set; }

        // Список всех жанров с флагом выбора
        public ObservableCollection<SelectableGenreModel> AllGenres { get; set; }

        private BookModel _selectedBook;
        public BookModel SelectedBook
        {
            get => _selectedBook;
            set { _selectedBook = value; OnPropertyChanged(); LoadSelectedBookToForm(); }
        }

        // Поля формы
        private string _formTitle;
        public string FormTitle
        { get => _formTitle; set { _formTitle = value; OnPropertyChanged(); } }

        private string _formDescription;
        public string FormDescription
        { get => _formDescription; set { _formDescription = value; OnPropertyChanged(); } }

        private string _formCoverPath;
        public string FormCoverPath
        { get => _formCoverPath; set { _formCoverPath = value; OnPropertyChanged(); } }

        private string _formContentText;
        public string FormContentText
        { get => _formContentText; set { _formContentText = value; OnPropertyChanged(); } }

        private string _unfreezeReason;
        public string UnfreezeReason
        { get => _unfreezeReason; set { _unfreezeReason = value; OnPropertyChanged(); } }

        // Состояния UI
        public bool IsEditMode => SelectedBook != null;
        public string FormHeader => IsEditMode ? "Редактирование книги" : "Добавление новой книги";
        public Visibility UnfreezePanelVisibility =>
            (SelectedBook != null && SelectedBook.IsFrozen == 1) ? Visibility.Visible : Visibility.Collapsed;

        // Команды
        public ICommand SaveBookCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand SubmitUnfreezeCommand { get; }

        public AuthorViewModel(int authorUserId)
        {
            _authorUserId = authorUserId;
            _repository = new AuthorRepository();
            MyBooks = new ObservableCollection<BookModel>();
            AllGenres = new ObservableCollection<SelectableGenreModel>();

            SaveBookCommand = new RelayCommand(SaveBook);
            ClearFormCommand = new RelayCommand(ResetForm);
            SubmitUnfreezeCommand = new RelayCommand(SubmitUnfreeze);
            BrowseCoverCommand = new RelayCommand(BrowseCover);

            LoadGenres();
            RefreshList();
        }

        private void LoadGenres()
        {
            AllGenres.Clear();
            var genres = _repository.GetAllGenres();
            foreach (var g in genres)
            {
                AllGenres.Add(new SelectableGenreModel
                {
                    GenreId = g.GenreId,
                    Name = g.Name,
                    IsSelected = false
                });
            }
        }

        private void BrowseCover()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите обложку книги",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*"
            };

            if (dialog.ShowDialog() == true)
                FormCoverPath = dialog.FileName;
        }

        private void RefreshList()
        {
            MyBooks.Clear();
            var books = _repository.GetBooksByAuthor(_authorUserId);
            foreach (var b in books) MyBooks.Add(b);
        }

        private void LoadSelectedBookToForm()
        {
            if (SelectedBook != null)
            {
                FormTitle = SelectedBook.Title;
                FormDescription = SelectedBook.Description;
                FormCoverPath = SelectedBook.CoverPath;
                FormContentText = SelectedBook.ContentText;

                // Загружаем жанры книги и помечаем выбранные
                var bookGenreIds = _repository.GetBookGenreIds(SelectedBook.BookId);
                foreach (var g in AllGenres)
                    g.IsSelected = bookGenreIds.Contains(g.GenreId);
            }
            else
            {
                ResetForm();
            }
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(FormHeader));
            OnPropertyChanged(nameof(UnfreezePanelVisibility));
        }

        private void SaveBook()
        {
            if (string.IsNullOrWhiteSpace(FormTitle))
            {
                MessageBox.Show("Название книги обязательно для заполнения!");
                return;
            }

            var selectedGenreIds = new System.Collections.Generic.List<int>(
                AllGenres.Where(g => g.IsSelected).Select(g => g.GenreId));

            if (IsEditMode)
            {
                SelectedBook.Title = FormTitle;
                SelectedBook.Description = FormDescription;
                SelectedBook.CoverPath = FormCoverPath;
                SelectedBook.ContentText = FormContentText;

                _repository.UpdateBook(SelectedBook);
                _repository.SaveBookGenres(SelectedBook.BookId, selectedGenreIds);
                MessageBox.Show("Книга успешно обновлена!", "Успех");
            }
            else
            {
                var newBook = new BookModel
                {
                    Title = FormTitle,
                    Description = FormDescription,
                    CoverPath = FormCoverPath,
                    ContentText = FormContentText,
                    AuthorUserId = _authorUserId
                };
                _repository.AddBook(newBook);

                // Получаем ID только что добавленной книги и сохраняем жанры
                if (selectedGenreIds.Count > 0)
                {
                    int newBookId = _repository.GetLastInsertedBookId(FormTitle, _authorUserId);
                    if (newBookId > 0)
                        _repository.SaveBookGenres(newBookId, selectedGenreIds);
                }

                MessageBox.Show("Новая книга успешно опубликована!", "Успех");
            }

            ResetForm();
            RefreshList();
        }

        private void SubmitUnfreeze()
        {
            if (SelectedBook == null || string.IsNullOrWhiteSpace(UnfreezeReason))
            {
                MessageBox.Show("Выберите книгу и опишите причину разморозки.");
                return;
            }
            _repository.SendBookUnfreezeRequest(_authorUserId, SelectedBook.BookId, UnfreezeReason);
            MessageBox.Show("Запрос на разморозку отправлен администраторам.", "Отправлено");
            UnfreezeReason = "";
        }

        private void ResetForm()
        {
            _selectedBook = null;
            FormTitle = "";
            FormDescription = "";
            FormCoverPath = "";
            FormContentText = "";
            UnfreezeReason = "";

            // Снимаем все выбранные жанры
            foreach (var g in AllGenres)
                g.IsSelected = false;

            OnPropertyChanged(nameof(SelectedBook));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(FormHeader));
            OnPropertyChanged(nameof(UnfreezePanelVisibility));
        }
    }
}

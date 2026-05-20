using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class AuthorViewModel : BaseViewModel
    {
        private readonly AuthorRepository _repository;
        private readonly int _authorUserId;

        public ObservableCollection<Book> MyBooks { get; set; }

        private Book _selectedBook;
        public Book SelectedBook
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

        // Принимает реальный userId автора из сессии
        public AuthorViewModel(int authorUserId)
        {
            _authorUserId = authorUserId;
            _repository = new AuthorRepository();
            MyBooks = new ObservableCollection<Book>();

            SaveBookCommand = new RelayCommand(SaveBook);
            ClearFormCommand = new RelayCommand(ResetForm);
            SubmitUnfreezeCommand = new RelayCommand(SubmitUnfreeze);

            RefreshList();
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

            if (IsEditMode)
            {
                SelectedBook.Title = FormTitle;
                SelectedBook.Description = FormDescription;
                SelectedBook.CoverPath = FormCoverPath;
                SelectedBook.ContentText = FormContentText;

                _repository.UpdateBook(SelectedBook);
                MessageBox.Show("Книга успешно обновлена!", "Успех");
            }
            else
            {
                var newBook = new Book
                {
                    Title = FormTitle,
                    Description = FormDescription,
                    CoverPath = FormCoverPath,
                    ContentText = FormContentText,
                    AuthorUserId = _authorUserId
                };
                _repository.AddBook(newBook);
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

            OnPropertyChanged(nameof(SelectedBook));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(FormHeader));
            OnPropertyChanged(nameof(UnfreezePanelVisibility));
        }
    }
}
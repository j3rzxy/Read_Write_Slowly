using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class BookDetailsViewModel : BaseViewModel
    {
        private readonly BookDetailsRepository _repository;
        private readonly int _bookId;
        private readonly User _currentUser;
        private readonly Action _goBack;   // делегат «вернуться назад»

        // Данные книги и отзывы
        public Book CurrentBook { get; set; }
        public ObservableCollection<Review> Reviews { get; set; }

        // Поля нового отзыва
        private string _newReviewText;
        public string NewReviewText
        {
            get => _newReviewText;
            set { _newReviewText = value; OnPropertyChanged(); }
        }

        private double _newReviewRating = 5;
        public double NewReviewRating
        {
            get => _newReviewRating;
            set { _newReviewRating = value; OnPropertyChanged(); }
        }

        // Видимость панели администратора
        public Visibility AdminControlsVisibility { get; set; }

        // Видимость кнопки «Удалить книгу» — только для автора книги или администратора
        public Visibility DeleteBookVisibility { get; private set; }

        // Команды
        public ICommand GoBackCommand { get; }
        public ICommand ReadBookCommand { get; }
        public ICommand DeleteBookCommand { get; }
        public ICommand SubmitReviewCommand { get; }
        public ICommand ComplainBookCommand { get; }
        public ICommand ComplainReviewCommand { get; }
        public ICommand FreezeBookCommand { get; }
        public ICommand FreezeReviewCommand { get; }

        // goBack — лямбда, которую передаёт CatalogViewModel / UserListsViewModel
        public BookDetailsViewModel(int bookId, User currentUser, Action goBack)
        {
            _repository = new BookDetailsRepository();
            _bookId = bookId;
            _currentUser = currentUser;
            _goBack = goBack;

            AdminControlsVisibility = (currentUser != null && currentUser.IsAdmin)
                                      ? Visibility.Visible
                                      : Visibility.Collapsed;

            CurrentBook = _repository.GetBookById(_bookId);
            Reviews = new ObservableCollection<Review>(_repository.GetReviewsByBookId(_bookId));

            // Кнопка удаления видна, если текущий пользователь — автор книги или администратор
            bool canDelete = currentUser != null &&
                             (currentUser.IsAdmin ||
                              (CurrentBook != null && CurrentBook.AuthorUserId == currentUser.UserId));
            DeleteBookVisibility = canDelete ? Visibility.Visible : Visibility.Collapsed;

            GoBackCommand       = new RelayCommand(GoBack);
            ReadBookCommand     = new RelayCommand(ReadBookText);
            DeleteBookCommand   = new RelayCommand(DeleteBook);
            SubmitReviewCommand = new RelayCommand(SubmitReview);
            ComplainBookCommand = new RelayCommand(ComplainBook);
            ComplainReviewCommand = new RelayCommand<Review>(ComplainReview);
            FreezeBookCommand   = new RelayCommand(FreezeBook);
            FreezeReviewCommand = new RelayCommand<Review>(FreezeReview);
        }

        private void GoBack() => _goBack?.Invoke();

        // Открывает встроенную читалку с текстом книги
        private void ReadBookText()
        {
            if (CurrentBook == null) return;

            var readerWindow = new View_.BookReaderWindow(
                CurrentBook.Title,
                CurrentBook.AuthorName,
                CurrentBook.ContentText);

            readerWindow.ShowDialog();
        }

        // Удаление книги — доступно автору или администратору
        private void DeleteBook()
        {
            if (CurrentBook == null) return;

            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите безвозвратно удалить книгу «{CurrentBook.Title}»?\n" +
                "Все отзывы, оценки и записи в списках чтения также будут удалены.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            bool deleted = _repository.DeleteBook(
                _bookId,
                _currentUser.UserId,
                _currentUser.IsAdmin);

            if (deleted)
            {
                MessageBox.Show("Книга успешно удалена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                GoBack();
            }
            else
            {
                MessageBox.Show("Не удалось удалить книгу. Убедитесь, что у вас есть права на это действие.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SubmitReview()
        {
            if (string.IsNullOrWhiteSpace(NewReviewText))
            {
                MessageBox.Show("Введите текст отзыва!");
                return;
            }

            _repository.AddReview(_currentUser.UserId, _bookId, NewReviewText, NewReviewRating);
            Reviews = new ObservableCollection<Review>(_repository.GetReviewsByBookId(_bookId));
            OnPropertyChanged(nameof(Reviews));
            NewReviewText = "";
        }

        private void ComplainBook()
        {
            _repository.SendComplaint(_currentUser.UserId, "book", _bookId, "Нарушение / Неприемлемый контент");
            MessageBox.Show("Жалоба на книгу отправлена администрации.");
        }

        private void ComplainReview(Review review)
        {
            if (review == null) return;
            _repository.SendComplaint(_currentUser.UserId, "review", review.ReviewId, "Оскорбление / Спам");
            MessageBox.Show("Жалоба на отзыв отправлена.");
        }

        private void FreezeBook()
        {
            _repository.FreezeBook(_bookId);
            MessageBox.Show("Книга заморожена и скрыта из каталога.");
            GoBack();
        }

        private void FreezeReview(Review review)
        {
            if (review == null) return;
            _repository.FreezeReview(review.ReviewId);
            Reviews.Remove(review);
            MessageBox.Show("Отзыв скрыт.");
        }
    }
}
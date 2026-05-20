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

        // Команды
        public ICommand GoBackCommand { get; }
        public ICommand ReadBookCommand { get; }
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

            GoBackCommand = new RelayCommand(GoBack);
            ReadBookCommand = new RelayCommand(ReadBookText);
            SubmitReviewCommand = new RelayCommand(SubmitReview);
            ComplainBookCommand = new RelayCommand(ComplainBook);
            ComplainReviewCommand = new RelayCommand<Review>(ComplainReview);
            FreezeBookCommand = new RelayCommand(FreezeBook);
            FreezeReviewCommand = new RelayCommand<Review>(FreezeReview);
        }

        private void GoBack() => _goBack?.Invoke();

        private void ReadBookText()
        {
            MessageBox.Show("Открытие встроенной читалки: " + CurrentBook?.Title);
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
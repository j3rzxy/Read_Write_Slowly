using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class BookDetailsViewModel : BaseViewModel
    {
        private readonly BookDetailsRepository _repository;
        private readonly int _bookId;

        // Данные книги и отзывы
        public Book CurrentBook { get; set; }
        public ObservableCollection<Review> Reviews { get; set; }

        // Поля для нового отзыва
        public string NewReviewText { get; set; }
        public double NewReviewRating { get; set; } = 5; // По умолчанию 5 звезд

        // Ролевая модель (из глобального сеанса пользователя)
        public Visibility AdminControlsVisibility { get; set; }

        // Команды
        public ICommand ReadBookCommand { get; }
        public ICommand SubmitReviewCommand { get; }
        public ICommand ComplainBookCommand { get; }
        public ICommand ComplainReviewCommand { get; }
        public ICommand FreezeBookCommand { get; }
        public ICommand FreezeReviewCommand { get; }

        public BookDetailsViewModel(int bookId, User currentUser)
        {
            _repository = new BookDetailsRepository();
            _bookId = bookId;

            // Проверка роли администратора
            AdminControlsVisibility = currentUser != null && currentUser.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Загрузка данных
            CurrentBook = _repository.GetBookById(_bookId);
            Reviews = new ObservableCollection<Review>(_repository.GetReviewsByBookId(_bookId));

            // Инициализация команд
            ReadBookCommand = new RelayCommand(ReadBookText);
            SubmitReviewCommand = new RelayCommand(SubmitReview);
            ComplainBookCommand = new RelayCommand(ComplainBook);
            ComplainReviewCommand = new RelayCommand<Review>(ComplainReview);
            FreezeBookCommand = new RelayCommand(FreezeBook);
            FreezeReviewCommand = new RelayCommand<Review>(FreezeReview);
        }

        private void ReadBookText()
        {
            // Переход на страницу полноэкранного чтения текста книги
            MessageBox.Show("Открытие встроенной читалки для книги: " + CurrentBook.Title);
        }

        private void SubmitReview()
        {
            if (string.IsNullOrWhiteSpace(NewReviewText))
            {
                MessageBox.Show("Введите текст отзыва!");
                return;
            }

            int userId = 1; // Заглушка текущего пользователя
            _repository.AddReview(userId, _bookId, NewReviewText, NewReviewRating);

            // Обновляем список отзывов на UI
            Reviews = new ObservableCollection<Review>(_repository.GetReviewsByBookId(_bookId));
            OnPropertyChanged(nameof(Reviews));
            NewReviewText = "";
            OnPropertyChanged(nameof(NewReviewText));
        }

        private void ComplainBook()
        {
            // В идеале открыть диалоговое окно для ввода причины
            string reason = "Нарушение авторских прав / Неприемлемый контент";
            int userId = 1;
            _repository.SendComplaint(userId, "Book", _bookId, reason);
            MessageBox.Show("Жалоба на книгу успешно отправлена администрации.");
        }

        private void ComplainReview(Review review)
        {
            if (review == null) return;
            _repository.SendComplaint(1, "Review", review.ReviewId, "Оскорбление / Спам");
            MessageBox.Show("Жалоба на отзыв отправлена.");
        }

        private void FreezeBook()
        {
            _repository.FreezeBook(_bookId);
            MessageBox.Show("Книга успешно заморожена и удалена из общего каталога.");
            // Логика возврата в каталог книг: MainNavigation.NavigateToCatalog();
        }

        private void FreezeReview(Review review)
        {
            if (review == null) return;
            _repository.FreezeReview(review.ReviewId);
            Reviews.Remove(review);
            MessageBox.Show("Отзыв скрыт и заморожен.");
        }
    }
}

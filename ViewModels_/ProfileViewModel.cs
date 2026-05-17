using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class ProfileViewModel : BaseViewModel
    {
        private readonly ProfileRepository _repository;
        private readonly int _currentUserId = 3; // Заглушка (в реальности берется ID залогиненного юзера, например Мария Читателева из скрипта)

        public User CurrentUser { get; set; }
        public ObservableCollection<Review> UserReviews { get; set; }

        // Видимость элементов управления
        public Visibility AuthorButtonVisibility => (CurrentUser != null && CurrentUser.RoleId == 1) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FrozenWarningVisibility => (CurrentUser != null && CurrentUser.IsFrozen) ? Visibility.Visible : Visibility.Collapsed;

        // Поле для текста апелляции
        private string _unfreezeReason;
        public string UnfreezeReason
        {
            get => _unfreezeReason;
            set { _unfreezeReason = value; OnPropertyChanged(); }
        }

        // Команды
        public ICommand ApplyForAuthorCommand { get; }
        public ICommand SubmitUnfreezeCommand { get; }

        public ProfileViewModel()
        {
            _repository = new ProfileRepository();

            ApplyForAuthorCommand = new RelayCommand(ApplyForAuthor);
            SubmitUnfreezeCommand = new RelayCommand(SubmitUnfreeze);

            LoadProfileData();
        }

        private void LoadProfileData()
        {
            CurrentUser = _repository.GetUserProfile(_currentUserId);
            var reviews = _repository.GetUserReviews(_currentUserId);

            UserReviews = new ObservableCollection<Review>(reviews);

            // Уведомляем UI о том, что данные загрузились
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(UserReviews));
            OnPropertyChanged(nameof(AuthorButtonVisibility));
            OnPropertyChanged(nameof(FrozenWarningVisibility));
        }

        private void ApplyForAuthor()
        {
            bool isSubmitted = _repository.CheckAndApplyForAuthor(_currentUserId);
            if (isSubmitted)
            {
                MessageBox.Show("Ваша заявка на роль Автора успешно отправлена администрации и находится на рассмотрении.", "Успех");
            }
            else
            {
                MessageBox.Show("Вы уже отправляли заявку ранее. Пожалуйста, дождитесь решения модератора.", "Инфо");
            }
        }

        private void SubmitUnfreeze()
        {
            if (string.IsNullOrWhiteSpace(UnfreezeReason))
            {
                MessageBox.Show("Пожалуйста, опишите причину для разморозки аккаунта.");
                return;
            }

            _repository.SendUnfreezeRequest(_currentUserId, UnfreezeReason);
            MessageBox.Show("Апелляция успешно отправлена администраторам.", "Отправлено");
            UnfreezeReason = "";
        }
    }
}

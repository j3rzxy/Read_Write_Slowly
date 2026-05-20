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
        private readonly int _currentUserId;

        public User CurrentUser { get; set; }
        public ObservableCollection<Review> UserReviews { get; set; }

        public Visibility AuthorButtonVisibility => (CurrentUser != null && CurrentUser.RoleId == 1)
                                                     ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FrozenWarningVisibility => (CurrentUser != null && CurrentUser.IsFrozen)
                                                     ? Visibility.Visible : Visibility.Collapsed;

        private string _unfreezeReason;
        public string UnfreezeReason
        {
            get => _unfreezeReason;
            set { _unfreezeReason = value; OnPropertyChanged(); }
        }

        public ICommand ApplyForAuthorCommand { get; }
        public ICommand SubmitUnfreezeCommand { get; }

        // Принимает реальный ID авторизованного пользователя
        public ProfileViewModel(int userId)
        {
            _currentUserId = userId;
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

            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(UserReviews));
            OnPropertyChanged(nameof(AuthorButtonVisibility));
            OnPropertyChanged(nameof(FrozenWarningVisibility));
        }

        private void ApplyForAuthor()
        {
            bool submitted = _repository.CheckAndApplyForAuthor(_currentUserId);
            if (submitted)
                MessageBox.Show("Заявка на роль Автора отправлена на рассмотрение.", "Успех");
            else
                MessageBox.Show("Вы уже отправляли заявку. Дождитесь решения модератора.", "Инфо");
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
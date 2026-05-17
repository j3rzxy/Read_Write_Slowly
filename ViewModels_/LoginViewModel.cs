using Read_Write_Slowly.ViewModels_;

namespace Read_Write_Slowly.Repositories_
{
    public class LoginViewModel : BaseViewModel // BaseViewModel реализует INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;

        public string Login { get; set; }
        // Пароль в WPF обычно обрабатывается через PasswordBox и передается в команду

        public RelayCommand LoginCommand { get; }

        public LoginViewModel()
        {
            _userRepository = new UserRepository();
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        }

        private void ExecuteLogin(object parameter)
        {
            // В реальном приложении пароль нужно хешировать перед отправкой в БД (например, SHA256)
            string passwordHash = parameter.ToString();

            var currentUser = _userRepository.AuthenticateUser(Login, passwordHash);

            if (currentUser != null)
            {
                if (currentUser.IsFrozen)
                {
                    // По ТЗ: показать окно предупреждения о заморозке
                    ShowFrozenWarning();
                }
                else
                {
                    // Успешная авторизация! 
                    // Сохраняем currentUser в глобальное состояние приложения
                    // Переключаем CurrentView в MainWindow на Каталог (CatalogPage)
                    NavigateToMainApp(currentUser);
                }
            }
            else
            {
                // Показать ошибку "Неверный логин или пароль"
            }
        }
    }
}

using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace Read_Write_Slowly.ViewModels_
{
    public class AuthViewModel : BaseViewModel
    {
        private readonly AuthRepository _repository;

        // Поля формы
        private string _loginOrEmail;
        public string LoginOrEmail { get => _loginOrEmail; set { _loginOrEmail = value; OnPropertyChanged(); } }

        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        private string _displayName;
        public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(); } }

        // Состояние: true = Регистрация, false = Вход
        private bool _isRegistrationMode;
        public bool IsRegistrationMode
        {
            get => _isRegistrationMode;
            set
            {
                _isRegistrationMode = value;
                OnPropertyChanged();
                // Уведомляем UI об изменении зависимых свойств
                OnPropertyChanged(nameof(FormHeader));
                OnPropertyChanged(nameof(SubmitButtonText));
                OnPropertyChanged(nameof(SwitchModeText));
                OnPropertyChanged(nameof(RegistrationFieldsVisibility));
            }
        }

        // Динамический текст для UI
        public string FormHeader => IsRegistrationMode ? "РЕГИСТРАЦИЯ" : "АВТОРИЗАЦИЯ";
        public string SubmitButtonText => IsRegistrationMode ? "Создать аккаунт" : "Войти";
        public string SwitchModeText => IsRegistrationMode ? "Уже есть аккаунт? Войти" : "Нет аккаунта? Зарегистрироваться";
        public Visibility RegistrationFieldsVisibility => IsRegistrationMode ? Visibility.Visible : Visibility.Collapsed;

        // Команды
        public ICommand SwitchModeCommand { get; }
        public ICommand SubmitCommand { get; }

        public AuthViewModel()
        {
            _repository = new AuthRepository();
            IsRegistrationMode = false; // По умолчанию открывается окно входа

            SwitchModeCommand = new RelayCommand(() => IsRegistrationMode = !IsRegistrationMode);
            SubmitCommand = new RelayCommand<object>(ExecuteSubmit);
        }

        private void ExecuteSubmit(object parameter)
        {
            // Извлекаем параметры из UI (нам придет массив: [PasswordBox, ТекущееОкно])
            var values = parameter as object[];
            if (values == null || values.Length < 2) return;

            var passwordBox = values[0] as PasswordBox;
            var currentWindow = values[1] as Window;

            string password = passwordBox?.Password;

            // Валидация базовых полей
            if (string.IsNullOrWhiteSpace(LoginOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, заполните логин и пароль!", "Предупреждение");
                return;
            }

            if (IsRegistrationMode)
            {
                // ---- ЛОГИКА РЕГИСТРАЦИИ ----
                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(DisplayName))
                {
                    MessageBox.Show("Заполните все поля для регистрации!", "Предупреждение");
                    return;
                }

                if (_repository.Register(LoginOrEmail, Email, password, DisplayName, out string error))
                {
                    MessageBox.Show("Регистрация успешно завершена! Теперь вы можете войти.", "Успех");
                    IsRegistrationMode = false; // переключаем на вход
                    passwordBox.Clear();
                }
                else
                {
                    MessageBox.Show(error, "Ошибка регистрации");
                }
            }
            else
            {
                // ---- ЛОГИКА ВХОДА ----
                User user = _repository.Login(LoginOrEmail, password);
                if (user != null)
                {
                    MessageBox.Show($"Добро пожаловать, {user.DisplayName}!", "Успешный вход");

                    // Открываем Главное окно приложения (MainWindow)
                    MainWindow mainWindow = new MainWindow(user); // ИСПРАВЛЕНО: передаём user, иначе DataContext = null
                    mainWindow.Show();

                    // Закрываем окно авторизации
                    currentWindow?.Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин/email или пароль!", "Ошибка доступа");
                }
            }
        }
    }
}
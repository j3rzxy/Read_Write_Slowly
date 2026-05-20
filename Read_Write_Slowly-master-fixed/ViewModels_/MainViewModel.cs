using Read_Write_Slowly.Models_;
using System.Windows;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class MainViewModel : BaseViewModel
    {
        // Текущий авторизованный пользователь (сессия)
        public User CurrentUser { get; set; }

        // Текущая страница в ContentControl
        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        // Видимость пунктов меню по роли
        public bool IsAuthorMenuVisible => CurrentUser != null && (CurrentUser.RoleId == 2 || CurrentUser.RoleId == 3);
        public bool IsAdminMenuVisible => CurrentUser != null && CurrentUser.RoleId == 3;

        // Команды навигации
        public ICommand OpenCatalogCommand { get; }
        public ICommand OpenProfileCommand { get; }
        public ICommand OpenMyListsCommand { get; }
        public ICommand OpenAuthorPanelCommand { get; }
        public ICommand OpenAdminPanelCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel(User user)
        {
            CurrentUser = user;

            // Стартовая страница — всегда Каталог
            CurrentViewModel = CreateCatalogViewModel();

            // Инициализация команд
            OpenCatalogCommand = new RelayCommand(() => CurrentViewModel = CreateCatalogViewModel());
            OpenProfileCommand = new RelayCommand(() => CurrentViewModel = new ProfileViewModel(CurrentUser.UserId));
            OpenMyListsCommand = new RelayCommand(() => CurrentViewModel = new UserListsViewModel(CurrentUser.UserId, NavigateTo));
            OpenAuthorPanelCommand = new RelayCommand(() => CurrentViewModel = new AuthorViewModel(CurrentUser.UserId));
            OpenAdminPanelCommand = new RelayCommand(() => CurrentViewModel = new AdminViewModel());

            LogoutCommand = new RelayCommand<Window>((window) =>
            {
                var authWindow = new View_.AuthWindow();
                authWindow.Show();
                window?.Close();
            });
        }

        // Централизованный метод навигации, передаётся дочерним ViewModel как делегат
        public void NavigateTo(object viewModel)
        {
            CurrentViewModel = viewModel;
        }

        // Фабричный метод — создаёт CatalogViewModel с правильным контекстом
        private CatalogViewModel CreateCatalogViewModel()
        {
            return new CatalogViewModel(CurrentUser, NavigateTo);
        }
    }
}
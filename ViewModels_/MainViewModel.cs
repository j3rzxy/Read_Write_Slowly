using Read_Write_Slowly.Models_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class MainViewModel : BaseViewModel
    {
        // Текущий пользователь (храним сессию)
        public User CurrentUser { get; set; }

        // Свойство, к которому привязан ContentControl (динамическая смена страниц)
        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        // Видимость разделов меню на основе Ролей
        // (Допустим: RoleId 1 = Читатель, 2 = Автор, 3 = Администратор)
        public bool IsAuthorMenuVisible => CurrentUser != null && (CurrentUser.RoleId == 2 || CurrentUser.RoleId == 3);
        public bool IsAdminMenuVisible => CurrentUser != null && CurrentUser.RoleId == 3;

        // Команды навигации
        public ICommand OpenCatalogCommand { get; }
        public ICommand OpenAuthorPanelCommand { get; }
        public ICommand OpenAdminPanelCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel(User user)
        {
            CurrentUser = user;

            // По умолчанию при входе открываем, например, Панель Автора или Каталог.
            // Если админ — откроем админку, если автор — панель автора, иначе — пустую/каталог.
            if (IsAdminMenuVisible)
                CurrentViewModel = new AdminViewModel();
            else if (IsAuthorMenuVisible)
                CurrentViewModel = new AuthorViewModel();
            else
                CurrentViewModel = null; // Здесь в будущем будет Каталог Книг (new CatalogViewModel())

            // Инициализация команд переключения страниц
            OpenAuthorPanelCommand = new RelayCommand(() => CurrentViewModel = new AuthorViewModel());
            OpenAdminPanelCommand = new RelayCommand(() => CurrentViewModel = new AdminViewModel());

            // Заглушка для каталога
            OpenCatalogCommand = new RelayCommand(() => MessageBox.Show("Экран каталога книг в разработке!", "Инфо"));

            // Команда выхода из аккаунта
            LogoutCommand = new RelayCommand<Window>((window) =>
            {
                // Открываем заново окно авторизации
                View_.AuthWindow authWindow = new View_.AuthWindow();
                authWindow.Show();

                // Закрываем главное окно
                window?.Close();
            });
        }
    }
}

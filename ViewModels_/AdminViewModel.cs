using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

// Явный алиас, чтобы не конфликтовал с Read_Write_Slowly.Book (EF-класс)
using BookModel = Read_Write_Slowly.Models_.Book;
using SelectableGenreModel = Read_Write_Slowly.Models_.SelectableGenre;

namespace Read_Write_Slowly.ViewModels_
{
    public class AdminViewModel : BaseViewModel
    {
        private readonly AdminRepository _repository;

        // ── Существующие коллекции ──────────────────────────────────────
        public ObservableCollection<RoleApplicationInfo> RoleApplications { get; set; }
        public ObservableCollection<UnfreezeRequestInfo> UnfreezeRequests { get; set; }
        public ObservableCollection<ComplaintInfo> Complaints { get; set; }

        // ── Команды управления заявками / жалобами ──────────────────────
        public ICommand ApproveRoleCommand { get; }
        public ICommand RejectRoleCommand { get; }
        public ICommand ApproveUnfreezeCommand { get; }
        public ICommand RejectUnfreezeCommand { get; }
        public ICommand ApproveComplaintCommand { get; }
        public ICommand RejectComplaintCommand { get; }

        // ── Форма добавления книги ──────────────────────────────────────
        public ObservableCollection<User> Authors { get; set; }
        public ObservableCollection<SelectableGenreModel> AllGenres { get; set; }

        private User _selectedAuthor;
        public User SelectedAuthor
        {
            get => _selectedAuthor;
            set { _selectedAuthor = value; OnPropertyChanged(); }
        }

        private string _adminBookTitle;
        public string AdminBookTitle
        {
            get => _adminBookTitle;
            set { _adminBookTitle = value; OnPropertyChanged(); }
        }

        private string _adminBookDescription;
        public string AdminBookDescription
        {
            get => _adminBookDescription;
            set { _adminBookDescription = value; OnPropertyChanged(); }
        }

        private string _adminBookCoverPath;
        public string AdminBookCoverPath
        {
            get => _adminBookCoverPath;
            set { _adminBookCoverPath = value; OnPropertyChanged(); }
        }

        private string _adminBookContent;
        public string AdminBookContent
        {
            get => _adminBookContent;
            set { _adminBookContent = value; OnPropertyChanged(); }
        }

        public ICommand AdminBrowseCoverCommand { get; }
        public ICommand AdminSaveBookCommand { get; }
        public ICommand AdminClearFormCommand { get; }

        // ────────────────────────────────────────────────────────────────

        public AdminViewModel()
        {
            _repository = new AdminRepository();

            RoleApplications = new ObservableCollection<RoleApplicationInfo>();
            UnfreezeRequests = new ObservableCollection<UnfreezeRequestInfo>();
            Complaints = new ObservableCollection<ComplaintInfo>();
            Authors = new ObservableCollection<User>();
            AllGenres = new ObservableCollection<SelectableGenreModel>();

            // Команды существующей логики
            ApproveComplaintCommand = new RelayCommand<ComplaintInfo>(ApproveComplaint);
            RejectComplaintCommand  = new RelayCommand<ComplaintInfo>(RejectComplaint);
            ApproveRoleCommand      = new RelayCommand<RoleApplicationInfo>(ApproveRole);
            RejectRoleCommand       = new RelayCommand<RoleApplicationInfo>(RejectRole);
            ApproveUnfreezeCommand  = new RelayCommand<UnfreezeRequestInfo>(ApproveUnfreeze);
            RejectUnfreezeCommand   = new RelayCommand<UnfreezeRequestInfo>(RejectUnfreeze);

            // Команды формы книги
            AdminBrowseCoverCommand = new RelayCommand(AdminBrowseCover);
            AdminSaveBookCommand    = new RelayCommand(AdminSaveBook);
            AdminClearFormCommand   = new RelayCommand(AdminClearForm);

            RefreshAll();
            LoadGenresAndAuthors();
        }

        // ── Инициализация ────────────────────────────────────────────────

        private void LoadGenresAndAuthors()
        {
            AllGenres.Clear();
            foreach (var g in _repository.GetAllGenres())
                AllGenres.Add(new SelectableGenreModel { GenreId = g.GenreId, Name = g.Name });

            Authors.Clear();
            foreach (var u in _repository.GetAllAuthors())
                Authors.Add(u);
        }

        private void RefreshAll()
        {
            RoleApplications.Clear();
            foreach (var a in _repository.GetPendingRoleApplications()) RoleApplications.Add(a);

            UnfreezeRequests.Clear();
            foreach (var r in _repository.GetActiveUnfreezeRequests()) UnfreezeRequests.Add(r);

            Complaints.Clear();
            foreach (var c in _repository.GetComplaints()) Complaints.Add(c);
        }

        // ── Обработчики заявок / жалоб (без изменений) ──────────────────

        private void ApproveRole(RoleApplicationInfo app)
        {
            if (app == null) return;
            _repository.ProcessRoleApplication(app.RoleApplicationId, app.UserId, app.RequestedRoleId, true);
            MessageBox.Show($"Пользователь {app.UserDisplayName} успешно переведен в роль {app.RequestedRoleName}!", "Успех");
            RefreshAll();
            LoadGenresAndAuthors(); // список авторов мог измениться
        }

        private void RejectRole(RoleApplicationInfo app)
        {
            if (app == null) return;
            _repository.ProcessRoleApplication(app.RoleApplicationId, app.UserId, app.RequestedRoleId, false);
            MessageBox.Show("Заявка отклонена.", "Инфо");
            RefreshAll();
        }

        private void ApproveUnfreeze(UnfreezeRequestInfo req)
        {
            if (req == null) return;
            _repository.ProcessUnfreezeRequest(req, true);
            MessageBox.Show($"{req.TargetName} успешно разморожен(а) для пользователя {req.UserDisplayName}.", "Выполнено");
            RefreshAll();
        }

        private void RejectUnfreeze(UnfreezeRequestInfo req)
        {
            if (req == null) return;
            _repository.ProcessUnfreezeRequest(req, false);
            MessageBox.Show("Апелляция отклонена. Ограничения остаются в силе.", "Инфо");
            RefreshAll();
        }

        private void ApproveComplaint(ComplaintInfo complaint)
        {
            if (complaint == null) return;
            _repository.ProcessComplaint(complaint, true);
            MessageBox.Show($"Жалоба одобрена. Объект (ID: {complaint.TargetId}) успешно заморожен.", "Выполнено");
            RefreshAll();
        }

        private void RejectComplaint(ComplaintInfo complaint)
        {
            if (complaint == null) return;
            _repository.ProcessComplaint(complaint, false);
            MessageBox.Show("Жалоба отклонена и удалена из очереди.", "Инфо");
            RefreshAll();
        }

        // ── Форма добавления книги ───────────────────────────────────────

        private void AdminBrowseCover()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите обложку книги",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*"
            };
            if (dialog.ShowDialog() == true)
                AdminBookCoverPath = dialog.FileName;
        }

        private void AdminSaveBook()
        {
            if (string.IsNullOrWhiteSpace(AdminBookTitle))
            {
                MessageBox.Show("Название книги обязательно!", "Ошибка");
                return;
            }
            if (SelectedAuthor == null)
            {
                MessageBox.Show("Выберите автора книги!", "Ошибка");
                return;
            }

            var newBook = new BookModel
            {
                Title = AdminBookTitle,
                Description = AdminBookDescription,
                CoverPath = AdminBookCoverPath,
                ContentText = AdminBookContent,
                AuthorUserId = SelectedAuthor.UserId
            };

            int newBookId = _repository.AddBook(newBook);

            var selectedGenreIds = new System.Collections.Generic.List<int>(
                AllGenres.Where(g => g.IsSelected).Select(g => g.GenreId));

            if (newBookId > 0 && selectedGenreIds.Count > 0)
                _repository.SaveBookGenres(newBookId, selectedGenreIds);

            MessageBox.Show($"Книга «{AdminBookTitle}» успешно добавлена!", "Успех");
            AdminClearForm();
        }

        private void AdminClearForm()
        {
            AdminBookTitle = "";
            AdminBookDescription = "";
            AdminBookCoverPath = "";
            AdminBookContent = "";
            SelectedAuthor = null;
            foreach (var g in AllGenres)
                g.IsSelected = false;
        }
    }
}

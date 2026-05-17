using Read_Write_Slowly.Models_;
using Read_Write_Slowly.Repositories_;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Read_Write_Slowly.ViewModels_
{
    public class AdminViewModel : BaseViewModel
    {
        private readonly AdminRepository _repository;

        // Коллекции данных для таблиц UI
        public ObservableCollection<RoleApplicationInfo> RoleApplications { get; set; }
        public ObservableCollection<UnfreezeRequestInfo> UnfreezeRequests { get; set; }

        // Команды для управления ролями
        public ICommand ApproveRoleCommand { get; }
        public ICommand RejectRoleCommand { get; }

        // Команды для управления разморозкой
        public ICommand ApproveUnfreezeCommand { get; }
        public ICommand RejectUnfreezeCommand { get; }

        public ObservableCollection<ComplaintInfo> Complaints { get; set; }

        public ICommand ApproveComplaintCommand { get; }
        public ICommand RejectComplaintCommand { get; }

        public AdminViewModel()
        {
            _repository = new AdminRepository();
            RoleApplications = new ObservableCollection<RoleApplicationInfo>();
            UnfreezeRequests = new ObservableCollection<UnfreezeRequestInfo>();
            
            Complaints = new ObservableCollection<ComplaintInfo>();
            ApproveComplaintCommand = new RelayCommand<ComplaintInfo>(ApproveComplaint);
            RejectComplaintCommand = new RelayCommand<ComplaintInfo>(RejectComplaint);

            // Инициализация команд
            ApproveRoleCommand = new RelayCommand<RoleApplicationInfo>(ApproveRole);
            RejectRoleCommand = new RelayCommand<RoleApplicationInfo>(RejectRole);
            ApproveUnfreezeCommand = new RelayCommand<UnfreezeRequestInfo>(ApproveUnfreeze);
            RejectUnfreezeCommand = new RelayCommand<UnfreezeRequestInfo>(RejectUnfreeze);

            RefreshAll();
        }

        private void RefreshAll()
        {
            // Обновляем список ролей
            RoleApplications.Clear();
            var apps = _repository.GetPendingRoleApplications();
            foreach (var a in apps) RoleApplications.Add(a);

            // Обновляем список апелляций
            UnfreezeRequests.Clear();
            var reqs = _repository.GetActiveUnfreezeRequests();
            foreach (var r in reqs) UnfreezeRequests.Add(r);

            //Обновляем список жалоб
            Complaints.Clear();
            var complaintsList = _repository.GetComplaints();
            foreach (var c in complaintsList) Complaints.Add(c);
        }

        // 1. Одобрить перевод в Авторы
        private void ApproveRole(RoleApplicationInfo app)
        {
            if (app == null) return;
            _repository.ProcessRoleApplication(app.RoleApplicationId, app.UserId, app.RequestedRoleId, true);
            MessageBox.Show($"Пользователь {app.UserDisplayName} успешно переведен в роль {app.RequestedRoleName}!", "Успех");
            RefreshAll();
        }

        // 2. Отклонить перевод в Авторы
        private void RejectRole(RoleApplicationInfo app)
        {
            if (app == null) return;
            _repository.ProcessRoleApplication(app.RoleApplicationId, app.UserId, app.RequestedRoleId, false);
            MessageBox.Show("Заявка отклонена.", "Инфо");
            RefreshAll();
        }

        // 3. Одобрить разморозку (вернуть доступ)
        private void ApproveUnfreeze(UnfreezeRequestInfo req)
        {
            if (req == null) return;
            _repository.ProcessUnfreezeRequest(req, true);
            MessageBox.Show($"{req.TargetName} успешно разморожен(а) для пользователя {req.UserDisplayName}.", "Выполнено");
            RefreshAll();
        }

        // 4. Отклонить разморозку (оставить бан)
        private void RejectUnfreeze(UnfreezeRequestInfo req)
        {
            if (req == null) return;
            _repository.ProcessUnfreezeRequest(req, false);
            MessageBox.Show("Апелляция отклонена. Ограничения остаются в силе.", "Инфо");
            RefreshAll();
        }

        // 5. Одобрить разморозку (оставить бан)
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
        }
    }
}
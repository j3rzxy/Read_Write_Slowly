using Read_Write_Slowly.Models_;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read_Write_Slowly.Repositories_
{
    public class AdminRepository
    {
        private readonly string _connectionString;

        public AdminRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        // 1. Получить все активные (pending) заявки на роли
        public List<RoleApplicationInfo> GetPendingRoleApplications()
        {
            var list = new List<RoleApplicationInfo>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT ra.RoleApplicationId, ra.UserId, u.DisplayName, u.Login, ra.RequestedRoleId, r.Name, ra.Status, ra.CreatedAt
                    FROM RoleApplication ra
                    INNER JOIN Users u ON ra.UserId = u.UserId
                    INNER JOIN Role r ON ra.RequestedRoleId = r.RoleId
                    WHERE ra.Status = 'pending'
                    ORDER BY ra.RoleApplicationId DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new RoleApplicationInfo
                        {
                            RoleApplicationId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            UserDisplayName = reader.GetString(2),
                            UserLogin = reader.GetString(3),
                            RequestedRoleId = reader.GetInt32(4),
                            RequestedRoleName = reader.GetString(5),
                            Status = reader.GetString(6),
                            CreatedAt = reader.GetString(7)
                        });
                    }
                }
            }
            return list;
        }

        // 2. Обработка заявки на роль (Одобрение / Отклонение)
        public void ProcessRoleApplication(int applicationId, int userId, int requestedRoleId, bool approve)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string newStatus = approve ? "approved" : "rejected";

                        // Обновляем статус заявки
                        string updateAppQuery = "UPDATE RoleApplication SET Status = @status WHERE RoleApplicationId = @appId";
                        using (SqlCommand cmd = new SqlCommand(updateAppQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@status", newStatus);
                            cmd.Parameters.AddWithValue("@appId", applicationId);
                            cmd.ExecuteNonQuery();
                        }

                        // Если одобрено — меняем роль самому пользователю
                        if (approve)
                        {
                            string updateUserQuery = "UPDATE Users SET RoleId = @roleId WHERE UserId = @userId";
                            using (SqlCommand cmd = new SqlCommand(updateUserQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@roleId", requestedRoleId);
                                cmd.Parameters.AddWithValue("@userId", userId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // 3. Получить все запросы на разморозку
        public List<UnfreezeRequestInfo> GetActiveUnfreezeRequests()
        {
            var list = new List<UnfreezeRequestInfo>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Используем LEFT JOIN на таблицу Book, чтобы достать название книги, если TargetType = 'book'
                string query = @"
                    SELECT ur.UnfreezeRequestId, ur.UserId, u.DisplayName, ur.TargetType, ur.TargetId, b.Title, ur.Reason, ur.CreatedAt
                    FROM UnfreezeRequest ur
                    INNER JOIN Users u ON ur.UserId = u.UserId
                    LEFT JOIN Book b ON ur.TargetId = b.BookId
                    ORDER BY ur.UnfreezeRequestId DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string targetType = reader.GetString(3);
                        string targetName = targetType == "account" ? "Весь аккаунт" : $"Книга: \"{reader.GetString(5)}\"";

                        list.Add(new UnfreezeRequestInfo
                        {
                            UnfreezeRequestId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            UserDisplayName = reader.GetString(2),
                            TargetType = targetType,
                            TargetId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            TargetName = targetName,
                            Reason = reader.GetString(6),
                            CreatedAt = reader.GetString(7)
                        });
                    }
                }
            }
            return list;
        }

        // 4. Выполнение разморозки (и удаление обработанного запроса)
        public void ProcessUnfreezeRequest(UnfreezeRequestInfo request, bool approve)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        if (approve)
                        {
                            if (request.TargetType == "account")
                            {
                                // Размораживаем пользователя (в Users это bit -> 0)
                                string query = "UPDATE Users SET IsFrozen = 0 WHERE UserId = @userId";
                                using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@userId", request.UserId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else if (request.TargetType == "book" && request.TargetId.HasValue)
                            {
                                // Размораживаем книгу (в Book это int -> 0)
                                string query = "UPDATE Book SET IsFrozen = 0 WHERE BookId = @bookId";
                                using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@bookId", request.TargetId.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Удаляем запрос из списка активных (так как он обработан)
                        string deleteQuery = "DELETE FROM UnfreezeRequest WHERE UnfreezeRequestId = @id";
                        using (SqlCommand cmd = new SqlCommand(deleteQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", request.UnfreezeRequestId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}

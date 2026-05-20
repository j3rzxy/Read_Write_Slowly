using Read_Write_Slowly.Models_;
using Book = Read_Write_Slowly.Models_.Book;
using Review = Read_Write_Slowly.Models_.Review;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace Read_Write_Slowly.Repositories_
{
    public class ProfileRepository
    {
        private readonly string _connectionString;

        public ProfileRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        // 1. Получить полную информацию о пользователе (включая имя роли)
        public User GetUserProfile(int userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT u.UserId, u.Login, u.Email, u.DisplayName, u.RegistrationDate, u.IsFrozen, u.RoleId, r.Name
                    FROM Users u
                    INNER JOIN Role r ON u.RoleId = r.RoleId
                    WHERE u.UserId = @userId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                UserId = reader.GetInt32(0),
                                Login = reader.GetString(1),
                                Email = reader.GetString(2),
                                DisplayName = reader.GetString(3),
                                RegistrationDate = reader.GetString(4), // В БД nvarchar(255)
                                IsFrozen = reader.GetBoolean(5),         // В БД bit -> bool
                                RoleId = reader.GetInt32(6),
                                RoleName = reader.GetString(7)          // Название роли из таблицы Role
                            };
                        }
                    }
                }
            }
            return null;
        }

        // 2. Получить все отзывы, оставленные этим пользователем
        public List<Review> GetUserReviews(int userId)
        {
            var reviews = new List<Review>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT r.ReviewId, r.BookId, b.Title, r.Text, r.Rating, r.CreatedAt
                    FROM Review r
                    INNER JOIN Book b ON r.BookId = b.BookId
                    WHERE r.UserId = @userId
                    ORDER BY r.ReviewId DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reviews.Add(new Review
                            {
                                ReviewId = reader.GetInt32(0),
                                BookId = reader.GetInt32(1),
                                BookTitle = reader.GetString(2),
                                Text = reader.GetString(3),
                                Rating = reader.GetDouble(4),
                                CreatedAt = reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
            return reviews;
        }

        // 3. Подать заявку на роль Автора (проверяет, нет ли уже активной заявки)
        public bool CheckAndApplyForAuthor(int userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Проверяем, нет ли уже отправленной заявки в статусе 'pending'
                string checkQuery = "SELECT COUNT(1) FROM RoleApplication WHERE UserId = @userId AND RequestedRoleId = 2 AND Status = 'pending'";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@userId", userId);
                    int exists = (int)checkCmd.ExecuteScalar();
                    if (exists > 0) return false; // Заявка уже на рассмотрении
                }

                // Если заявки нет — создаем новую. По ТЗ ID для роли Автора = 2
                // Получаем максимальный ID для генерации (так как PK не IDENTITY в скрипте)
                string idQuery = "SELECT ISNULL(MAX(RoleApplicationId), 0) + 1 FROM RoleApplication";
                int newId = 1;
                using (SqlCommand idCmd = new SqlCommand(idQuery, conn))
                {
                    newId = (int)idCmd.ExecuteScalar();
                }

                string insertQuery = @"
                    INSERT INTO RoleApplication (RoleApplicationId, UserId, RequestedRoleId, Status, CreatedAt) 
                    VALUES (@id, @userId, 2, 'pending', @createdAt)";

                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@id", newId);
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    insertCmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        // 4. Отправить запрос на разморозку аккаунта
        public void SendUnfreezeRequest(int userId, string reason)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string idQuery = "SELECT ISNULL(MAX(UnfreezeRequestId), 0) + 1 FROM UnfreezeRequest";
                int newId = 1;
                using (SqlCommand idCmd = new SqlCommand(idQuery, conn))
                {
                    newId = (int)idCmd.ExecuteScalar();
                }

                string query = @"
                    INSERT INTO UnfreezeRequest (UnfreezeRequestId, UserId, TargetType, TargetId, Reason, CreatedAt) 
                    VALUES (@id, @userId, 'account', NULL, @reason, @createdAt)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@reason", reason);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
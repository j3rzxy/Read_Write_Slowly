using Read_Write_Slowly.Models_;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using Book = Read_Write_Slowly.Models_.Book;
using Review = Read_Write_Slowly.Models_.Review;

namespace Read_Write_Slowly.Repositories_
{
    public class ProfileRepository
    {
        public string cleanConnectionString;
        public ProfileRepository()
        {
            var entityString = ConfigurationManager.ConnectionStrings["ShutIKrolEntities"].ConnectionString;

            var builder = new EntityConnectionStringBuilder(entityString);
            cleanConnectionString = builder.ProviderConnectionString;
        }

        // 1. Получить полную информацию о пользователе (включая имя роли)
        public User GetUserProfile(int userId)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = @"
                    SELECT u.UserId, u.Login, u.Email, u.DisplayName, u.RegistrationDate, u.IsFrozen, u.RoleId, r.Name
                    FROM Users u
                    INNER JOIN Role r ON u.RoleId = r.RoleId
                    WHERE u.UserId = @userId";

                using (SqlCommand cmd = new SqlCommand(query, connection))
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
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = @"
                    SELECT r.ReviewId, r.BookId, b.Title, r.Text, r.Rating, r.CreatedAt
                    FROM Review r
                    INNER JOIN Book b ON r.BookId = b.BookId
                    WHERE r.UserId = @userId
                    ORDER BY r.ReviewId DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
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
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();

                // Проверяем, нет ли уже отправленной заявки в статусе 'pending'
                string checkQuery = "SELECT COUNT(1) FROM RoleApplication WHERE UserId = @userId AND RequestedRoleId = 2 AND Status = 'pending'";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@userId", userId);
                    int exists = (int)checkCmd.ExecuteScalar();
                    if (exists > 0) return false; // Заявка уже на рассмотрении
                }

                // Если заявки нет — создаем новую. По ТЗ ID для роли Автора = 2
                // Получаем максимальный ID для генерации (так как PK не IDENTITY в скрипте)
                string idQuery = "SELECT ISNULL(MAX(RoleApplicationId), 0) + 1 FROM RoleApplication";
                int newId = 1;
                using (SqlCommand idCmd = new SqlCommand(idQuery, connection))
                {
                    newId = (int)idCmd.ExecuteScalar();
                }

                string insertQuery = @"
                    INSERT INTO RoleApplication (UserId, RequestedRoleId, Status, CreatedAt) 
                    VALUES (@userId, @requestedRoleId, 'pending', @createdAt)";

                using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.Parameters.AddWithValue("@requestedRoleId", 2);
                    insertCmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    insertCmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        // 4. Отправить запрос на разморозку аккаунта
        public void SendUnfreezeRequest(int userId, string reason)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();

                string idQuery = "SELECT ISNULL(MAX(UnfreezeRequestId), 0) + 1 FROM UnfreezeRequest";
                int newId = 1;
                using (SqlCommand idCmd = new SqlCommand(idQuery, connection))
                {
                    newId = (int)idCmd.ExecuteScalar();
                }

                string query = @"
                    INSERT INTO UnfreezeRequest (TargetType, TargetId, Reason, CreatedAt) 
                    VALUES ('account', NULL, @reason, @createdAt)";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@reason", reason);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
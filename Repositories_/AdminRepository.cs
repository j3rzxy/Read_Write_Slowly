using Read_Write_Slowly.Models_;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BookModel  = Read_Write_Slowly.Models_.Book;
using GenreModel = Read_Write_Slowly.Models_.Genre;
using UserModel  = Read_Write_Slowly.Models_.User;

namespace Read_Write_Slowly.Repositories_
{
    public class AdminRepository
    {
        public string cleanConnectionString;
        public AdminRepository()
        {
            var entityString = ConfigurationManager.ConnectionStrings["ShutIKrolEntities"].ConnectionString;

            var builder = new EntityConnectionStringBuilder(entityString);
            cleanConnectionString = builder.ProviderConnectionString;
        }

        // 1. Получить все активные (pending) заявки на роли
        public List<RoleApplicationInfo> GetPendingRoleApplications()
        {
            var list = new List<RoleApplicationInfo>();
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
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
                            CreatedAt = reader.GetDateTime(7)
                        });
                    }
                }
            }
            return list;
        }

        // 2. Обработка заявки на роль (Одобрение / Отклонение)
        public void ProcessRoleApplication(int applicationId, int userId, int requestedRoleId, bool approve)
        {
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
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
        public List<ComplaintInfo> GetComplaints()
        {
            var list = new List<ComplaintInfo>();
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
            {
                conn.Open();

                string query = @"
    SELECT c.ComplaintId, u.DisplayName, c.TargetType, c.TargetId, c.Reason, c.CreatedAt,
           ISNULL(b.Title, ISNULL(r.Text, 'Удаленный объект')) AS TargetDescription,
           ru.DisplayName AS ReviewAuthorName
    FROM Complaint c
    JOIN Users u ON c.UserId = u.UserId
    LEFT JOIN Book b ON c.TargetType = 'book' AND c.TargetId = b.BookId
    LEFT JOIN Review r ON c.TargetType = 'review' AND c.TargetId = r.ReviewId
    LEFT JOIN Users ru ON r.UserId = ru.UserId
    ORDER BY c.ComplaintId ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ComplaintInfo
                        {
                            ComplaintId = reader.GetInt32(0),
                            ReporterName = reader.GetString(1),
                            TargetType = reader.GetString(2),
                            TargetId = reader.GetInt32(3),
                            Reason = reader.GetString(4),
                            CreatedAt = reader.IsDBNull(5) ? "" : reader.GetDateTime(5).ToString("dd.MM.yyyy HH:mm"),
                            TargetDescription = reader.GetString(6),
                            ReviewAuthorName = reader.IsDBNull(7) ? null : reader.GetString(7)
                        });
                    }
                }
            }
            return list;
        }

        // 3. Обработка жалобы (Удовлетворить или Отклонить)
        public void ProcessComplaint(ComplaintInfo complaint, bool approve)
        {
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Если жалоба одобрена, замораживаем объект
                        if (approve)
                        {
                            if (complaint.TargetType == "book")
                            {
                                string freezeBookQuery = "UPDATE Book SET IsFrozen = 1 WHERE BookId = @targetId";
                                using (SqlCommand cmd = new SqlCommand(freezeBookQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@targetId", complaint.TargetId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else if (complaint.TargetType == "review")
                            {
                                // Предполагается, что вы добавили поле IsFrozen в таблицу Review
                                string freezeReviewQuery = "UPDATE Review SET IsFrozen = 1 WHERE ReviewId = @targetId";
                                using (SqlCommand cmd = new SqlCommand(freezeReviewQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@targetId", complaint.TargetId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // В любом случае (одобрили или отклонили) удаляем жалобу из очереди
                        string deleteComplaintQuery = "DELETE FROM Complaint WHERE ComplaintId = @complaintId";
                        using (SqlCommand cmd = new SqlCommand(deleteComplaintQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@complaintId", complaint.ComplaintId);
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

        // 4. Получить все запросы на разморозку
        public List<UnfreezeRequestInfo> GetActiveUnfreezeRequests()
        {
            var list = new List<UnfreezeRequestInfo>();
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
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
                        string bookTitle = reader.IsDBNull(5) ? "Без названия" : reader.GetString(5);
                        string targetName = targetType == "account" ? "Весь аккаунт" : $"Книга: \"{bookTitle}\"";

                        list.Add(new UnfreezeRequestInfo
                        {
                            UnfreezeRequestId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            UserDisplayName = reader.GetString(2),
                            TargetType = targetType,
                            TargetId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            TargetName = targetName,
                            Reason = reader.GetString(6),
                            CreatedAt = reader.GetDateTime(7)
                        });
                    }
                }
            }
            return list;
        }

        // 5. Выполнение разморозки (и удаление обработанного запроса)
        public void ProcessUnfreezeRequest(UnfreezeRequestInfo request, bool approve)
        {
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
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
        // Получить все жанры
        public List<GenreModel> GetAllGenres()
        {
            var genres = new List<GenreModel>();
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
            {
                conn.Open();
                string query = "SELECT GenreId, Name FROM Genre ORDER BY Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        genres.Add(new GenreModel
                        {
                            GenreId = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }
            return genres;
        }

        // Добавить книгу (от имени выбранного автора или администратора)
        public int AddBook(BookModel book)
        {
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
            {
                conn.Open();
                string query = @"
                    INSERT INTO Book (Title, Description, CoverPath, ContentText, AuthorUserId, IsFrozen)
                    OUTPUT INSERTED.BookId
                    VALUES (@title, @desc, @cover, @content, @authorId, 0)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", book.Title);
                    cmd.Parameters.AddWithValue("@desc", book.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cover", string.IsNullOrWhiteSpace(book.CoverPath)
                        ? "pack://application:,,,/Resources/no_cover.png" : book.CoverPath);
                    cmd.Parameters.AddWithValue("@content", book.ContentText ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@authorId", book.AuthorUserId);

                    object result = cmd.ExecuteScalar();
                    return result != null ? (int)result : -1;
                }
            }
        }

        // Сохранить жанры книги
        public void SaveBookGenres(int bookId, List<int> selectedGenreIds)
        {
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string deleteQuery = "DELETE FROM BookGenre WHERE BookId = @bookId";
                        using (SqlCommand cmd = new SqlCommand(deleteQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@bookId", bookId);
                            cmd.ExecuteNonQuery();
                        }

                        foreach (int genreId in selectedGenreIds)
                        {
                            string insertQuery = "INSERT INTO BookGenre (BookId, GenreId) VALUES (@bookId, @genreId)";
                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@bookId", bookId);
                                cmd.Parameters.AddWithValue("@genreId", genreId);
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

        // Получить список всех авторов (для выбора при добавлении книги)
        public List<UserModel> GetAllAuthors()
        {
            var users = new List<UserModel>();
            using (SqlConnection conn = new SqlConnection(cleanConnectionString))
            {
                conn.Open();
                string query = @"
                    SELECT u.UserId, u.DisplayName, u.Login
                    FROM Users u
                    INNER JOIN Role r ON u.RoleId = r.RoleId
                    WHERE r.Name = 'Author'
                    ORDER BY u.DisplayName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserModel
                        {
                            UserId = reader.GetInt32(0),
                            DisplayName = reader.GetString(1),
                            Login = reader.GetString(2)
                        });
                    }
                }
            }
            return users;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;

namespace Read_Write_Slowly.Repositories_
{
    public class BookDetailsRepository
    {
        public string cleanConnectionString;
        public BookDetailsRepository()
        {
            var entityString = ConfigurationManager.ConnectionStrings["ShutIKrolEntities"].ConnectionString;

            var builder = new EntityConnectionStringBuilder(entityString);
            cleanConnectionString = builder.ProviderConnectionString;
        }

        // 1. Загрузка полной информации об одной книге (включая строку с жанрами)
        public Book GetBookById(int bookId)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                // ИСПРАВЛЕНО: b.AuthorId → b.AuthorUserId (реальное имя столбца в БД)
                string query = @"
                    SELECT b.BookId, b.Title, b.Description, b.CoverPath, u.DisplayName,
                           (SELECT STRING_AGG(g.Name, ', ') 
                            FROM BookGenre bg 
                            JOIN Genre g ON bg.GenreId = g.GenreId 
                            WHERE bg.BookId = b.BookId) as Genres
                    FROM Book b
                    INNER JOIN Users u ON b.AuthorUserId = u.UserId
                    WHERE b.BookId = @bookId";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Book
                            {
                                BookId = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                CoverPath = reader.IsDBNull(3) ? "pack://application:,,,/Resources/no_cover.png" : reader.GetString(3),
                                AuthorName = reader.GetString(4),
                                Genres = reader.IsDBNull(5) ? new List<string>(): new List<string>(reader.GetString(5).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                            };
                        }
                    }
                }
            }
            return null;
        }

        // 2. Получение незамороженных отзывов к книге
        public List<Review> GetReviewsByBookId(int bookId)
        {
            var reviews = new List<Review>();
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = @"
                    SELECT r.ReviewId, r.UserId, u.DisplayName, r.Text, r.Rating, r.CreatedAt, r.IsFrozen
                    FROM Review r
                    JOIN Users u ON r.UserId = u.UserId
                    WHERE r.BookId = @bookId AND r.IsFrozen = 0
                    ORDER BY r.CreatedAt DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reviews.Add(new Review
                            {
                                ReviewId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                // ИСПРАВЛЕНО: UserDisplayName теперь видно — это поле Models_.Review
                                UserDisplayName = reader.GetString(2),
                                Text = reader.GetString(3),
                                Rating = reader.GetDouble(4),
                                CreatedAt = reader.GetDateTime(5),
                                IsFrozen = reader.GetBoolean(6)
                            });
                        }
                    }
                }
            }
            return reviews;
        }

        // 3. Добавление отзыва
        public void AddReview(int userId, int bookId, string text, double rating)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = "INSERT INTO Review (UserId, BookId, Text, Rating, IsFrozen, CreatedAt) VALUES (@userId, @bookId, @text, @rating, 0, @createdAt)";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    cmd.Parameters.AddWithValue("@text", text);
                    cmd.Parameters.AddWithValue("@rating", rating);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 4. Универсальный метод отправки жалоб (на книгу или отзыв)
        public void SendComplaint(int userId, string targetType, int targetId, string reason)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = "INSERT INTO Complaint (UserId, TargetType, TargetId, Reason, Status, CreatedAt) VALUES (@userId, @targetType, @targetId, @reason, 'pending', @createdAt)";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@targetType", targetType);
                    cmd.Parameters.AddWithValue("@targetId", targetId);
                    cmd.Parameters.AddWithValue("@reason", reason);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 5. Администрирование: заморозка книги
        public void FreezeBook(int bookId)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = "UPDATE Book SET IsFrozen = 1 WHERE BookId = @bookId";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 6. Администрирование: заморозка отзыва
        public void FreezeReview(int reviewId)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = "UPDATE Review SET IsFrozen = 1 WHERE ReviewId = @reviewId";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@reviewId", reviewId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;

namespace Read_Write_Slowly.Repositories_
{
    public class AuthorRepository
    {
        public string cleanConnectionString;
        public AuthorRepository()
        {
            var entityString = ConfigurationManager.ConnectionStrings["ShutIKrolEntities"].ConnectionString;

            var builder = new EntityConnectionStringBuilder(entityString);
            cleanConnectionString = builder.ProviderConnectionString;
        }

        // 1. Получить все книги автора (включая замороженные)
        public List<Book> GetBooksByAuthor(int authorUserId)
        {
            var books = new List<Book>();
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = @"
                    SELECT BookId, Title, Description, CoverPath, ContentText, IsFrozen 
                    FROM Book 
                    WHERE AuthorUserId = @authorId
                    ORDER BY BookId DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@authorId", authorUserId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                BookId = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                CoverPath = reader.IsDBNull(3) ? "pack://application:,,,/Resources/no_cover.png" : reader.GetString(3),
                                ContentText = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                IsFrozen = reader.GetInt32(5),
                                AuthorUserId = authorUserId
                            });
                        }
                    }
                }
            }
            return books;
        }

        // 2. Добавление новой книги (с ручной генерацией BookId)
        public void AddBook(Book book)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();

                // Генерируем новый BookId
                string idQuery = "SELECT ISNULL(MAX(BookId), 0) + 1 FROM Book";
                int newBookId = 1;
                using (SqlCommand idCmd = new SqlCommand(idQuery, connection))
                {
                    newBookId = (int)idCmd.ExecuteScalar();
                }

                string query = @"
                    INSERT INTO Book (Title, Description, CoverPath, ContentText, AuthorUserId, IsFrozen) 
                    VALUES (@title, @desc, @cover, @content, @authorId, 0)";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@title", book.Title);
                    cmd.Parameters.AddWithValue("@desc", book.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cover", string.IsNullOrWhiteSpace(book.CoverPath) ? "pack://application:,,,/Resources/no_cover.png" : book.CoverPath);
                    cmd.Parameters.AddWithValue("@content", book.ContentText ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@authorId", book.AuthorUserId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 3. Обновление существующей книги
        public void UpdateBook(Book book)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = @"
                    UPDATE Book 
                    SET Title = @title, Description = @desc, CoverPath = @cover, ContentText = @content 
                    WHERE BookId = @bookId AND AuthorUserId = @authorId";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@title", book.Title);
                    cmd.Parameters.AddWithValue("@desc", book.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cover", book.CoverPath);
                    cmd.Parameters.AddWithValue("@content", book.ContentText ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@bookId", book.BookId);
                    cmd.Parameters.AddWithValue("@authorId", book.AuthorUserId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 4. Отправить запрос на разморозку КНИГИ (TargetType = 'book', TargetId = BookId)
        public void SendBookUnfreezeRequest(int userId, int bookId, string reason)
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
                    INSERT INTO UnfreezeRequest (UserId, TargetId, TargetType, Reason, CreatedAt) 
                    VALUES (@userId, @bookId, 'book', @reason, @createdAt)";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    cmd.Parameters.AddWithValue("@reason", reason);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

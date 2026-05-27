using Read_Write_Slowly.Models_;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;

using BookModel  = Read_Write_Slowly.Models_.Book;
using GenreModel = Read_Write_Slowly.Models_.Genre;

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
        public List<BookModel> GetBooksByAuthor(int authorUserId)
        {
            var books = new List<BookModel>();
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
                            books.Add(new BookModel
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
        public void AddBook(BookModel book)
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
        public void UpdateBook(BookModel book)
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

        // 4. Получить все жанры из БД
        public List<GenreModel> GetAllGenres()
        {
            var genres = new List<GenreModel>();
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = "SELECT GenreId, Name FROM Genre ORDER BY Name";
                using (SqlCommand cmd = new SqlCommand(query, connection))
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

        // 5. Получить жанры конкретной книги (список GenreId)
        public List<int> GetBookGenreIds(int bookId)
        {
            var ids = new List<int>();
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = "SELECT GenreId FROM BookGenre WHERE BookId = @bookId";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            ids.Add(reader.GetInt32(0));
                    }
                }
            }
            return ids;
        }

        // 6. Сохранить жанры книги (удаляем старые, вставляем новые)
        public void SaveBookGenres(int bookId, List<int> selectedGenreIds)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Удаляем все текущие жанры книги
                        string deleteQuery = "DELETE FROM BookGenre WHERE BookId = @bookId";
                        using (SqlCommand cmd = new SqlCommand(deleteQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@bookId", bookId);
                            cmd.ExecuteNonQuery();
                        }

                        // Вставляем выбранные жанры
                        foreach (int genreId in selectedGenreIds)
                        {
                            string insertQuery = @"
                                INSERT INTO BookGenre (BookId, GenreId)
                                VALUES (@bookId, @genreId)";
                            using (SqlCommand cmd = new SqlCommand(insertQuery, connection, transaction))
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

        // 7. Получить BookId только что добавленной книги (последняя по Title + AuthorUserId)
        public int GetLastInsertedBookId(string title, int authorUserId)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = @"
                    SELECT TOP 1 BookId FROM Book
                    WHERE Title = @title AND AuthorUserId = @authorId
                    ORDER BY BookId DESC";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@authorId", authorUserId);
                    object result = cmd.ExecuteScalar();
                    return result != null ? (int)result : -1;
                }
            }
        }

        // 8. Отправить запрос на разморозку КНИГИ (TargetType = 'book', TargetId = BookId)
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

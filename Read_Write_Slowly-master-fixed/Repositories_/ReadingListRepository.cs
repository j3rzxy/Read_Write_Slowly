using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;

namespace Read_Write_Slowly.Repositories_
{
    public class ReadingListRepository
    {
        public string cleanConnectionString;
        public ReadingListRepository()
        {
            var entityString = ConfigurationManager.ConnectionStrings["ShutIKrolEntities"].ConnectionString;

            var builder = new EntityConnectionStringBuilder(entityString);
            cleanConnectionString = builder.ProviderConnectionString;
        }

        // 1. Загрузка книг из конкретного списка пользователя с учетом поиска, фильтра и сортировки
        public List<Book> GetUserReadingList(int userId, string listStatus, string searchText, int? genreId, string sortBy)
        {
            var books = new List<Book>();

            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();

                // Выбираем книги, которые добавлены у конкретного пользователя в определенный статус (ReadingList.Status)
                // Книги, замороженные авторами/админами, скрываем
                string query = @"
                    SELECT b.BookId, b.Title, b.Description, b.CoverPath, 
                           u.DisplayName AS AuthorName, 
                           ISNULL(AVG(r.Rating), 0) AS AvgRating
                    FROM ReadingList rl
                    INNER JOIN Book b ON rl.BookId = b.BookId
                    INNER JOIN Users u ON b.AuthorUserId = u.UserId
                    LEFT JOIN Review r ON b.BookId = r.BookId AND r.IsFrozen = 0
                    WHERE rl.UserId = @userId AND rl.Status = @status AND b.IsFrozen = 0";

                // Динамические условия
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query += " AND (b.Title LIKE @search OR u.DisplayName LIKE @search) ";
                }

                if (genreId.HasValue)
                {
                    query += " AND b.BookId IN (SELECT BookId FROM BookGenre WHERE GenreId = @genreId) ";
                }

                query += " GROUP BY b.BookId, b.Title, b.Description, b.CoverPath, u.DisplayName ";

                // Сортировка
                if (sortBy == "Rating")
                    query += " ORDER BY AvgRating DESC";
                else
                    query += " ORDER BY b.Title ASC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@status", listStatus);

                    if (!string.IsNullOrWhiteSpace(searchText))
                        cmd.Parameters.AddWithValue("@search", $"%{searchText}%");

                    if (genreId.HasValue)
                        cmd.Parameters.AddWithValue("@genreId", genreId.Value);

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
                                AuthorName = reader.GetString(4),
                                AverageRating = Math.Round(reader.GetDouble(5), 1)
                            });
                        }
                    }
                }
            }
            return books;
        }

        // 2. Быстрое изменение статуса книги (перемещение в другой список)
        public void MoveBookToAnotherList(int userId, int bookId, string newStatus)
        {
            using (SqlConnection connection = new SqlConnection(cleanConnectionString))
            {
                connection.Open();
                string query = "UPDATE ReadingList SET Status = @newStatus WHERE UserId = @userId AND BookId = @bookId";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@newStatus", newStatus);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using Book = Read_Write_Slowly.Models_.Book;
using Review = Read_Write_Slowly.Models_.Review;
using Genre = Read_Write_Slowly.Models_.Genre;

namespace Read_Write_Slowly.Repositories_
{
    public class BookRepository
    {
        private readonly string _connectionString;

        public BookRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        // 1. Получение списка жанров для ComboBox фильтрации
        public List<Genre> GetGenres()
        {
            var genres = new List<Genre>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT GenreId, Name FROM Genre ORDER BY Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        genres.Add(new Genre { GenreId = reader.GetInt32(0), Name = reader.GetString(1) });
                    }
                }
            }
            return genres;
        }

        // 2. Динамический поиск, фильтрация и сортировка книг для каталога
        public List<Book> GetFilteredBooks(string searchText, int? genreId, string sortBy)
        {
            var books = new List<Book>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // ИСПРАВЛЕНО: b.AuthorId → b.AuthorUserId во всех частях запроса
                string query = @"
                    SELECT b.BookId, b.Title, b.Description, b.CoverPath, 
                           u.DisplayName AS AuthorName, 
                           ISNULL(AVG(r.Rating), 0) AS AvgRating
                    FROM Book b
                    INNER JOIN Users u ON b.AuthorUserId = u.UserId
                    LEFT JOIN Review r ON b.BookId = r.BookId AND r.IsFrozen = 0
                    WHERE b.IsFrozen = 0 ";

                if (!string.IsNullOrWhiteSpace(searchText))
                    query += " AND (b.Title LIKE @search OR u.DisplayName LIKE @search) ";

                if (genreId.HasValue)
                    query += " AND b.BookId IN (SELECT BookId FROM BookGenre WHERE GenreId = @genreId) ";

                query += " GROUP BY b.BookId, b.Title, b.Description, b.CoverPath, u.DisplayName ";

                if (sortBy == "Rating")
                    query += " ORDER BY AvgRating DESC";
                else
                    query += " ORDER BY b.Title ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
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

        // 3. Добавление или перемещение книги в список чтения пользователя
        public void AddBookToReadingList(int userId, int bookId, string listStatus)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string checkQuery = "SELECT COUNT(1) FROM ReadingList WHERE UserId = @userId AND BookId = @bookId";
                bool exists = false;

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@userId", userId);
                    checkCmd.Parameters.AddWithValue("@bookId", bookId);
                    exists = (int)checkCmd.ExecuteScalar() > 0;
                }

                string query = exists
                    ? "UPDATE ReadingList SET Status = @status WHERE UserId = @userId AND BookId = @bookId"
                    : "INSERT INTO ReadingList (UserId, BookId, Status) VALUES (@userId, @bookId, @status)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    cmd.Parameters.AddWithValue("@status", listStatus);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
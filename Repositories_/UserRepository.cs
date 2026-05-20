using Read_Write_Slowly.Models_;
using System.Configuration;
using System.Data.SqlClient;

namespace Read_Write_Slowly.Repositories_
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository()
        {
            _connectionString = ConfigurationManager
                .ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        public User AuthenticateUser(string login, string passwordHash)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT u.UserId, u.Login, u.DisplayName, u.RoleId, u.IsFrozen,
                           u.Email, u.RegistrationDate, r.Name AS RoleName
                    FROM   Users u
                    JOIN   Roles r ON r.RoleId = u.RoleId
                    WHERE  u.Login = @login AND u.PasswordHash = @passwordHash";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapUser(reader);
                    }
                }
            }
            return null;
        }

        // Получение пользователя по его Id (используется в UserListsViewModel)
        public User GetUserById(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT u.UserId, u.Login, u.DisplayName, u.RoleId, u.IsFrozen,
                           u.Email, u.RegistrationDate, r.Name AS RoleName
                    FROM   Users u
                    JOIN   Roles r ON r.RoleId = u.RoleId
                    WHERE  u.UserId = @userId";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapUser(reader);
                    }
                }
            }
            return null;
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetInt32(0),
                Login = reader.GetString(1),
                DisplayName = reader.GetString(2),
                RoleId = reader.GetInt32(3),
                IsFrozen = reader.GetBoolean(4),
                Email = reader.IsDBNull(5) ? "" : reader.GetString(5),
                RegistrationDate = reader.IsDBNull(6) ? "" : reader.GetDateTime(6).ToString("dd.MM.yyyy"),
                RoleName = reader.IsDBNull(7) ? "" : reader.GetString(7),
            };
        }
    }
}
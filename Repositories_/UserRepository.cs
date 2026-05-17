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
            // Получаем строку подключения из App.config
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        public User AuthenticateUser(string login, string passwordHash)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Запрос проверяет логин и хеш пароля, и если они верны — возвращает данные пользователя
                string query = @"SELECT UserId, Login, DisplayName, RoleId, IsFrozen 
                                 FROM Users 
                                 WHERE Login = @login AND PasswordHash = @passwordHash";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@passwordHash", passwordHash);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // Если запись найдена
                        {
                            return new User
                            {
                                UserId = reader.GetInt32(0),
                                Login = reader.GetString(1),
                                DisplayName = reader.GetString(2),
                                RoleId = reader.GetInt32(3),
                                IsFrozen = reader.GetBoolean(4)
                            };
                        }
                    }
                }
            }
            return null; // Если логин или пароль неверные
        }
    }
}

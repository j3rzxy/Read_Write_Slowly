using Read_Write_Slowly.Models_;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Read_Write_Slowly.Repositories_
{
    public class AuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        // 1. Авторизация пользователя
        public User Login(string loginOrEmail, string password)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Проверяем совпадение по логину ИЛИ email
                string query = @"
                    SELECT UserId, Login, Email, DisplayName, RegistrationDate, IsFrozen, RoleId 
                    FROM Users 
                    WHERE (Login = @loginOrEmail OR Email = @loginOrEmail) AND Password = @password";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@loginOrEmail", loginOrEmail);
                    cmd.Parameters.AddWithValue("@password", password); // В учебных целях храним строкой. 

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
                                RegistrationDate = reader.GetString(4),
                                IsFrozen = reader.GetBoolean(5),
                                RoleId = reader.GetInt32(6)
                            };
                        }
                    }
                }
            }
            return null; // Если пользователь не найден или пароль неверный
        }

        // 2. Регистрация нового пользователя
        public bool Register(string login, string email, string password, string displayName, out string errorMessage)
        {
            errorMessage = string.Empty;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Проверка: существует ли уже пользователь с таким логином или email
                string checkQuery = "SELECT COUNT(1) FROM Users WHERE Login = @login OR Email = @email";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@login", login);
                    checkCmd.Parameters.AddWithValue("@email", email);
                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        errorMessage = "Пользователь с таким логином или Email уже зарегистрирован!";
                        return false;
                    }
                }

                // Генерация нового UserId (так как IDENTITY отключен в скрипте)
                string idQuery = "SELECT ISNULL(MAX(UserId), 0) + 1 FROM Users";
                int newUserId = 1;
                using (SqlCommand idCmd = new SqlCommand(idQuery, conn))
                {
                    newUserId = (int)idCmd.ExecuteScalar();
                }

                // Вставка нового пользователя. По умолчанию: RoleId = 1 (Читатель), IsFrozen = 0 (Не заморожен)
                string insertQuery = @"
                    INSERT INTO Users (UserId, Login, Email, Password, DisplayName, RegistrationDate, IsFrozen, RoleId) 
                    VALUES (@userId, @login, @email, @password, @displayName, @regDate, 0, 1)";

                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@userId", newUserId);
                    insertCmd.Parameters.AddWithValue("@login", login);
                    insertCmd.Parameters.AddWithValue("@email", email);
                    insertCmd.Parameters.AddWithValue("@password", password);
                    insertCmd.Parameters.AddWithValue("@displayName", displayName);
                    insertCmd.Parameters.AddWithValue("@regDate", DateTime.Now.ToString("yyyy-MM-dd"));

                    insertCmd.ExecuteNonQuery();
                }
            }
            return true;
        }
    }
}

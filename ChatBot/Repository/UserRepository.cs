using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Dapper;
using MySql.Data.MySqlClient;

namespace ChatBot.Repository
{
    public class UserRepository:IUser
    {
        private readonly string _connectionString;
        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Users> GetUserList()
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    var user = connection.QueryAsync<Users>(
                        "SELECT id, text, category, is_active, created_at, updated_at FROM question_group"
                    ).Result.ToList();

                    return user;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}

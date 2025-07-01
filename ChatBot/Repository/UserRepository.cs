using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ChatBot.Repository
{
    public class UserRepository : IUser
    {
        private readonly string _connectionString;
        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Users> GetUserList()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var user = connection.QueryAsync<Users>(
                        "SELECT Id, Name, Email, Mobile, Role, IsPremium, CreatedAt, UpdatedAt FROM Users"
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
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Dapper;
using MySqlConnector;

namespace ChatBot.Repository
{
    public class UserSignupRepository: IUserSignUp
    {
        private readonly string _connectionString;
        public UserSignupRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public int SaveUser(Users users)
        {

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                MySqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    users.Id = connection.QueryAsync<int>(@"INSERT INTO users(name, password_hash,mobile, ,role ,is_premium ,updated_at ,created_at)
                                                                        VALUES(name, @password_hash , @mobile , @role , @is_premium , @updated_at ,@created_at ); SELECT LAST_INSERT_ID();", 
                        new {name=users.Name, mobile = users.Mobile,role=users.Role, is_premium=users.IsPremium, password_hash= users.PasswordHash, created_at = users.CreatedAt, updated_at = users.UpdatedAt}, transaction: transaction).Result.FirstOrDefault();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                return users.Id;
            }
            return 0;
        }
    }
}

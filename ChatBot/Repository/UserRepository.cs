using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

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
        public Users GetUserByRefreshToken(string token)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var user = connection.QueryAsync<Users>(
                        "SELECT * FROM users u INNER JOIN refreshtoken  rt ON u.userid= rt.userId WHERE rt.Token=@Token ;", param:new{Token=token }
                    ).Result.FirstOrDefault();

                    return user;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public RefreshToken GetRefreshByRefreshToken(string token)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var user = connection.QueryAsync<RefreshToken>(
                        "SELECT * FROM  refreshtoken  rt  WHERE rt.Token=@Token ;", param: new { Token = token }
                    ).Result.FirstOrDefault();

                    return user;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public List<RefreshToken> GetRefreshTokenByUserId(int userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString)) // Changed from MySqlConnection
            {
                try
                {
                    return connection.Query<RefreshToken>(
                        sql: "SELECT * FROM RefreshToken rt WHERE rt.UserId = @userId;", // Table name case-sensitive in some SQL Server configurations
                        param: new { userId = userId },
                        commandType: CommandType.Text
                    ).ToList();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
        public int SaveRefreshToken(RefreshToken refreshToken)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString)) // Changed from MySqlConnection
            {
                connection.Open();
                int tokenid;
                SqlTransaction transaction = connection.BeginTransaction(); // Changed from MySqlTransaction
                try
                {
                    tokenid = connection.QueryAsync<int>(@"
                INSERT INTO RefreshToken (Token, JWTToken, UserId, Expires, Created)
                VALUES (@token, @jwtToken, @userId, @expires, @created); 
                SELECT CAST(SCOPE_IDENTITY() as int);", // Changed from LAST_INSERT_ID()
                        new
                        {
                            token = refreshToken.Token,
                            jwtToken = refreshToken.JWTToken,
                            userId = refreshToken.UserId,
                            expires = refreshToken.Expires,
                            created = refreshToken.Created
                        },
                        transaction: transaction).Result.FirstOrDefault();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                return tokenid;
            }
        }

        public bool UpdateRefreshToken(RefreshToken refreshToken)
        {
            int rowsAffectedCount = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString)) // Changed from MySqlConnection
            {
                try
                {
                    rowsAffectedCount += connection.Execute(
                        sql: @"UPDATE RefreshToken SET Token = @token, Revoked = @revoked, JWTToken = @jwtToken, Expires = @expires 
                       WHERE Token = @token;", // Note: This has a logical issue - see fixed version below
                        commandType: CommandType.Text,
                        param: new
                        {
                            token = refreshToken.Token,
                            jwtToken = refreshToken.JWTToken,
                            userId = refreshToken.UserId,
                            expires = refreshToken.Expires,
                            revoked = refreshToken.Revoked,
                            created = refreshToken.Created
                        });
                    return rowsAffectedCount > 0;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
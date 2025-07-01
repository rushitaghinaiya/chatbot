using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using static ChatBot.Models.Common.AesEncryptionHelper;
using Dapper;
using Model.ViewModels;
using Microsoft.Data.SqlClient;
using System.Data;
using VRMDBCommon2023;

namespace ChatBot.Repository
{
    public class UserSignupRepository : IUserSignUp
    {
        private readonly string _connectionString;
        public UserSignupRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int SaveUser(Users users)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    users.Mobile = Encrypt(users.Mobile);
                    users.Id = connection.QueryAsync<int>(@"
                        INSERT INTO Users(Name, PasswordHash, Mobile, Role, IsPremium, UpdatedAt, CreatedAt)
                        VALUES(@name, @password_hash, @mobile, @role, @is_premium, @updated_at, @created_at); 
                        SELECT CAST(SCOPE_IDENTITY() as int);",
                        new
                        {
                            name = users.Name,
                            mobile = users.Mobile,
                            role = users.Role,
                            is_premium = users.IsPremium,
                            password_hash = users.PasswordHash,
                            created_at = users.CreatedAt,
                            updated_at = users.UpdatedAt
                        }, transaction: transaction).Result.FirstOrDefault();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                return users.Id;
            }
        }

        public int SaveLoginLog(LoginLogVM loginLog)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    loginLog.LogId = connection.QueryAsync<int>(@"
                        INSERT INTO LoginLogs (UserId, LoginTime, Status, FailureReason, CreatedAt) 
                        VALUES (@UserId, @LoginTime, @Status, @FailureReason, @CreatedAt);
                        SELECT CAST(SCOPE_IDENTITY() as int);",
                        new
                        {
                            loginLog.UserId,
                            loginLog.LoginTime,
                            loginLog.Status,
                            loginLog.FailureReason,
                            loginLog.CreatedAt
                        }, transaction: transaction).Result.FirstOrDefault();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                return loginLog.LogId;
            }
        }

        public int SaveAdminLoginLog(AdminLoginLog loginLog)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    var insertedId = connection.QueryAsync<int>(@"
                        INSERT INTO AdminLoginLogs (AdminId, LoginTime, Actions)
                        VALUES (@AdminId, @LoginTime, @Actions);
                        SELECT CAST(SCOPE_IDENTITY() as int);",
                        new
                        {
                            loginLog.AdminId,
                            loginLog.LoginTime,
                            loginLog.Actions
                        }, transaction: transaction).Result.FirstOrDefault();

                    transaction.Commit();
                    return insertedId;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public bool UpdateLoginStatus(int logId, string status, string? failureReason = null)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    var rowsAffected = connection.Execute(@"
                        UPDATE LoginLogs
                        SET Status = @Status,
                            FailureReason = @FailureReason
                        WHERE LogId = @LogId;",
                        new
                        {
                            LogId = logId,
                            Status = status,
                            FailureReason = failureReason
                        }, transaction: transaction);

                    transaction.Commit();
                    return rowsAffected > 0;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public Users IsExistUser(string mobile)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    mobile = Encrypt(mobile);
                    Users user = new Users();
                    user = connection.Query<Users>(
                        sql: "SELECT * FROM Users u WHERE u.Mobile = @mobile;",
                        param: new { mobile },
                        commandType: CommandType.Text
                    ).FirstOrDefault();
                    if (user != null)
                    {
                        user.Mobile = Decrypt(user.Mobile);
                    }
                    return user;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public Task<int> SaveOTP(OTPVM otpVM)
        {
            int rowsAffectedCount = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    rowsAffectedCount += connection.Query<int>(
                     @"INSERT INTO AuthenticationOtp(UserId, OtpNumber, OtpTime, CreatedAt)
                       VALUES(@UserId, @OtpNumber, @OtpTime, @CreatedAt);
                       SELECT CAST(SCOPE_IDENTITY() as int);",
                        commandType: CommandType.Text,
                        param: new
                        {
                            otpVM.UserId,
                            otpVM.OtpNumber,
                            OtpTime = DateTime.Now,
                            CreatedAt = DateTime.Now
                        }, transaction: transaction).FirstOrDefault();
                    transaction.Commit();
                    return Task.FromResult(rowsAffectedCount);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public OTPVM GetOTP(OTPVM otpVM)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    otpVM = connection.QueryAsync<OTPVM>(@"
                        SELECT TOP 1 OtpNumber, OtpTime 
                        FROM AuthenticationOtp 
                        WHERE UserId = @userid 
                        ORDER BY Id DESC",
                        param: new { userid = otpVM.UserId }).Result.FirstOrDefault();
                    return otpVM;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
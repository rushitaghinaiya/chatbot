using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using static ChatBot.Models.Common.AesEncryptionHelper;
using Dapper;
using Model.ViewModels;
using MySqlConnector;
using Org.BouncyCastle.Asn1.Cms;
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

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                MySqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    users.Mobile = Encrypt(users.Mobile);
                    users.Id = connection.QueryAsync<int>(@"INSERT INTO users(name, password_hash,mobile,role ,is_premium ,updated_at ,created_at)
                                                                        VALUES(name, @password_hash , @mobile , @role , @is_premium , @updated_at ,@created_at ); SELECT LAST_INSERT_ID();",
                        new { name = users.Name, mobile = users.Mobile, role = users.Role, is_premium = users.IsPremium, password_hash = users.PasswordHash, created_at = users.CreatedAt, updated_at = users.UpdatedAt }, transaction: transaction).Result.FirstOrDefault();
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

        public int SaveLoginLog(LoginLogVM loginLog)
        {

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                MySqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    loginLog.LogId = connection.QueryAsync<int>(@"INSERT INTO Login_Logs (UserId,LoginTime,Status,FailureReason,CreatedAt) 
                        VALUES (@UserId,@LoginTime,@Status,@FailureReason,@CreatedAt);SELECT LAST_INSERT_ID();",
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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                MySqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    var insertedId = connection.QueryAsync<int>(@"
                INSERT INTO admin_login_logs (id, admin_id, login_time, actions)
                VALUES (@Id, @AdminId, @LoginTime, @Actions);
                SELECT LAST_INSERT_ID();",
                        new
                        {
                            loginLog.Id,
                            loginLog.AdminId,
                            loginLog.LoginTime,
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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                MySqlTransaction transaction = connection.BeginTransaction();
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

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    mobile = Encrypt(mobile);
                    Users user = new Users();
                    user = connection.Query<Users>(
                        sql: "SELECT * FROM users u WHERE u.mobile=@mobile;",
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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    MySqlTransaction transaction = connection.BeginTransaction();
                    rowsAffectedCount += connection.Query<int>(
                     @"INSERT INTO AuthenticationOtp(UserId,OtpNumber,OtpTime,CreatedAt)
							VALUES(@UserId,@OtpNumber,@OtpTime,@CreatedAt);
                        SELECT LAST_INSERT_ID();",
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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    otpVM = connection.QueryAsync<OTPVM>("SELECT a.otpnumber,a.otptime FROM authenticationotp a WHERE a.userid=@userid ORDER BY a.id DESC LIMIT 1", param: new
                    {
                        userid = otpVM.UserId
                    }).Result.FirstOrDefault();
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

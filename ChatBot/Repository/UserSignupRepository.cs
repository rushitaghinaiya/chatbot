using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using static ChatBot.Models.Common.AesEncryptionHelper;
using Dapper;
using Model.ViewModels;
using Microsoft.Data.SqlClient;
using System.Data;
using VRMDBCommon2023;
using ClosedXML.Excel;
using ChatBot.Models.Common;

namespace ChatBot.Repository
{
    public class UserSignupRepository : IUserSignUp
    {
        private readonly string _connectionString;
        private readonly AppSettings _appSettings;
        public UserSignupRepository(string connectionString,AppSettings appSettings)
        {
            _appSettings = appSettings;
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
                        sql: "SELECT * FROM Users u WHERE u.mobile = @mobile;",
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

        public Users IsExistEmail(string EmailId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    Users user = new Users();
                    user = connection.Query<Users>(
                        sql: "SELECT * FROM Users u WHERE u.email = @EmailId;",
                        param: new { EmailId },
                        commandType: CommandType.Text
                    ).FirstOrDefault();
                   
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
            int newId = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    if (otpVM.UserId > 0)
                    {
                        // Insert with UserId
                        newId = connection.Query<int>(
                            @"INSERT INTO AuthenticationOtp (EmailId, UserId, OtpNumber, OtpTime, CreatedAt)
                      VALUES (@EmailId, @UserId, @OtpNumber, @OtpTime, @CreatedAt);
                      SELECT CAST(SCOPE_IDENTITY() as int);",
                            new
                            {
                                otpVM.EmailId,
                                otpVM.UserId,
                                otpVM.OtpNumber,
                                OtpTime = DateTime.Now,
                                CreatedAt = DateTime.Now
                            },
                            commandType: CommandType.Text,
                            transaction: transaction
                        ).FirstOrDefault();
                    }
                    else
                    {
                        // Insert without UserId
                        newId = connection.Query<int>(
                            @"INSERT INTO AuthenticationOtp (EmailId, OtpNumber, OtpTime, CreatedAt)
                      VALUES (@EmailId, @OtpNumber, @OtpTime, @CreatedAt);
                      SELECT CAST(SCOPE_IDENTITY() as int);",
                            new
                            {
                                otpVM.EmailId,
                                otpVM.OtpNumber,
                                OtpTime = DateTime.Now,
                                CreatedAt = DateTime.Now
                            },
                            commandType: CommandType.Text,
                            transaction: transaction
                        ).FirstOrDefault();
                    }

                    transaction.Commit();
                    return Task.FromResult(newId);
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
                        WHERE EmailId = @emailid OR UserId=@userId
                        ORDER BY Id DESC",
                        param: new { emailid = otpVM.EmailId,userId=otpVM.UserId}).Result.FirstOrDefault();
                    return otpVM;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public (bool exists, UserDetailsExcel user) VerifyEmail(string email)
        {
            if (!File.Exists(_appSettings.UserFilePath))
                throw new FileNotFoundException("Excel file not found!", _appSettings.UserFilePath);

            using var workbook = new XLWorkbook(_appSettings.UserFilePath);
            var worksheet = workbook.Worksheets.First();

            foreach (var row in worksheet.RowsUsed().Skip(1)) // skip header row
            {
                var cellValue = row.Cell(4).GetString().Trim().ToLower(); // Column D (Login Email)

                if (cellValue == email.Trim().ToLower())
                {
                    var user= new UserDetailsExcel
                    {
                        DisplayName = row.Cell(1).GetString().Trim(),
                        FirstName = row.Cell(2).GetString().Trim(),
                        LastName = row.Cell(3).GetString().Trim(),
                        LoginEmail = row.Cell(4).GetString().Trim(),
                        Phone = row.Cell(5).GetString().Trim(),
                        Courses = row.Cell(6).GetString().Trim(),
                        IsMembership = bool.TryParse(row.Cell(7).GetString().Trim(), out var result) && result
                    };
                    return (true, user);
                }
            }
            return (false, null);
        }

        public (bool exists, UserDetailsExcel user) VerifyEmail(string email, UserDetailsExcel newUser = null)
        {
            if (!File.Exists(_appSettings.UserFilePath))
                throw new FileNotFoundException("Excel file not found!", _appSettings.UserFilePath);

            using var workbook = new XLWorkbook(_appSettings.UserFilePath);
            var worksheet = workbook.Worksheets.First();

            foreach (var row in worksheet.RowsUsed().Skip(1)) // skip header row
            {
                var cellValue = row.Cell(4).GetString().Trim().ToLower(); // Column D (Login Email)

                if (cellValue == email.Trim().ToLower())
                {
                    var user = new UserDetailsExcel
                    {
                        DisplayName = row.Cell(1).GetString().Trim(),
                        FirstName = row.Cell(2).GetString().Trim(),
                        LastName = row.Cell(3).GetString().Trim(),
                        LoginEmail = row.Cell(4).GetString().Trim(),
                        Phone = row.Cell(5).GetString().Trim(),
                        Courses = row.Cell(6).GetString().Trim(),
                        IsMembership = bool.TryParse(row.Cell(7).GetString().Trim(), out var result) && result
                    };
                    return (true, user);
                }
            }

            // If not found, add new user if provided
            if (newUser != null)
            {
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1; // find last used row
                var newRow = worksheet.Row(lastRow + 1);

                newRow.Cell(1).Value = newUser.DisplayName ?? "";
                newRow.Cell(2).Value = newUser.FirstName ?? "";
                newRow.Cell(3).Value = newUser.LastName ?? "";
                newRow.Cell(4).Value = newUser.LoginEmail ?? "";
                newRow.Cell(5).Value = newUser.Phone ?? "";
                newRow.Cell(6).Value = newUser.Courses ?? "";
                newRow.Cell(7).Value = newUser.IsMembership;

                workbook.Save();
                return (false, newUser);
            }

            return (false, null);
        }


        public void SyncUsers(string uploadedFilePath)
        {
            if (!File.Exists(_appSettings.UserFilePath))
                throw new FileNotFoundException("Excel1 not found!", _appSettings.UserFilePath);

            if (!File.Exists(uploadedFilePath))
                throw new FileNotFoundException("Excel2 not found!", uploadedFilePath);

            using var workbook1 = new XLWorkbook(_appSettings.UserFilePath); // Excel1
            var worksheet1 = workbook1.Worksheets.First();

            using var workbook2 = new XLWorkbook(uploadedFilePath); // Excel2
            var worksheet2 = workbook2.Worksheets.First();

            // Build a set of existing emails in Excel1
            var existingEmails = worksheet1.RowsUsed()
                                           .Skip(1) // skip header
                                           .Select(r => r.Cell(4).GetString().Trim().ToLower())
                                           .ToHashSet();

            // Iterate over users in Excel2
            foreach (var row in worksheet2.RowsUsed().Skip(1)) // skip header row
            {
                var email = row.Cell(4).GetString().Trim().ToLower();

                // If user not found in Excel1, add them
                if (!existingEmails.Contains(email))
                {
                    var lastRow = worksheet1.LastRowUsed()?.RowNumber() ?? 1;
                    var newRow = worksheet1.Row(lastRow + 1);

                    newRow.Cell(1).Value = row.Cell(1).GetString().Trim(); // DisplayName
                    newRow.Cell(2).Value = row.Cell(2).GetString().Trim(); // FirstName
                    newRow.Cell(3).Value = row.Cell(3).GetString().Trim(); // LastName
                    newRow.Cell(4).Value = row.Cell(4).GetString().Trim(); // LoginEmail
                    newRow.Cell(5).Value = row.Cell(5).GetString().Trim(); // Phone
                    newRow.Cell(6).Value = row.Cell(6).GetString().Trim(); // Courses
                    newRow.Cell(7).Value = row.Cell(7).GetBoolean(); // IsMembership
                }
            }

            // Save changes back to Excel1
            workbook1.Save();
        }


    }
}
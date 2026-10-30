using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using static ChatBot.Models.Common.AesEncryptionHelper;

namespace ChatBot.Repository
{
    public class UserMgmtRepository : IUserMgmtService
    {
        private readonly string _connectionString;
        private readonly AppSettings _appsettings;
        public UserMgmtRepository(string connectionString, AppSettings appsetting)
        {
            _connectionString = connectionString;
            _appsettings = appsetting;
        }
        public async Task<FreeUsersOverviewDto> GetFreeUsersOverviewAsync()
        {

            // Step 1: Read users from Excel
            var usersFromExcel = ReadExcel(); // List<UserDetails>

            using (var connection = new SqlConnection(_connectionString))
            {
                // Step 2: Get sessions, query history, and system limits from DB
                var userSessions = (await connection.QueryAsync<UserSession>(
                    "SELECT EmailId FROM UserSessions")).ToList();

                var queryHistory = (await connection.QueryAsync<QueryHistoryDto>(
                    @"SELECT  * FROM QueryHistory q
                         CROSS APPLY OPENJSON(q.ChatJson)
                         WITH (
                             QueryText NVARCHAR(MAX) '$.queryText',
                             ResponseText NVARCHAR(MAX) '$.responseText',
                             ResponseTime FLOAT '$.responseTime',
                             Topic NVARCHAR(200) '$.topic',
                             Status NVARCHAR(50) '$.status'
                         ) j
                         WHERE CAST(q.Timestamp AS DATE) = CAST(GETDATE() AS DATE);")).ToList();

                var freeUserQueryLimit = await connection.ExecuteScalarAsync<int>(
                    "SELECT FreeUserQueryLimit FROM SystemLimits");

                // Step 3: Filter free users from Excel (IsMembership == false)
                var freeUsers = usersFromExcel.Where(u => !u.IsMembership && string.IsNullOrEmpty(u.Courses)).ToList();

                // Active free users = free users having any session
                var activeUsers = freeUsers
                    .Count(u => userSessions.Any(s => s.EmailId == u.LoginEmail));

                // Inactive free users = free users with no session
                var inactiveUsers = freeUsers
                    .Count(u => !userSessions.Any(s => s.EmailId == u.LoginEmail));

                // Free users with >80% of FreeQueryLimit used
                var highUsageUsers = freeUsers.Count(u =>
                {
                    var queryCount = queryHistory.Count(q => q.EmailId == u.LoginEmail);
                    return queryCount >= 0.8 * freeUserQueryLimit;
                });

                // Step 4: Return result
                return new FreeUsersOverviewDto
                {
                    TotalFreeUsers = freeUsers.Count(),
                    ActiveUsers = activeUsers,
                    InactiveUsers = inactiveUsers,
                    HighUsageUsers = highUsageUsers
                };
            }
        }
        public async Task<PaidUsersOverviewDto> GetPaidUsersOverviewAsync()
        {

            // Step 1: Read users from Excel
            var usersFromExcel = ReadExcel(); // List<UserDetails>

            using (var connection = new SqlConnection(_connectionString))
            {
                // Step 2: Get sessions, query history, and system limits from DB
                var userSessions = (await connection.QueryAsync<UserSession>(
                    "SELECT EmailId FROM UserSessions")).ToList();

                var queryHistory = (await connection.QueryAsync<QueryHistoryDto>(
                    @"SELECT  * FROM QueryHistory q
                         CROSS APPLY OPENJSON(q.ChatJson)
                         WITH (
                             QueryText NVARCHAR(MAX) '$.queryText',
                             ResponseText NVARCHAR(MAX) '$.responseText',
                             ResponseTime FLOAT '$.responseTime',
                             Topic NVARCHAR(200) '$.topic',
                             Status NVARCHAR(50) '$.status'
                         ) j
                         WHERE CAST(q.Timestamp AS DATE) = CAST(GETDATE() AS DATE);")).ToList();

                var paidUserQueryLimit = 100;

                // Step 3: Filter free users from Excel (IsMembership == false)
                var paidUsers = usersFromExcel.Where(u => u.IsMembership || !string.IsNullOrEmpty(u.Courses)).ToList();

                // Active free users = free users having any session
                var activeUsers = paidUsers
                    .Count(u => userSessions.Any(s => s.EmailId == u.LoginEmail));

                // Inactive free users = free users with no session
                var inactiveUsers = paidUsers
                    .Count(u => !userSessions.Any(s => s.EmailId == u.LoginEmail));

                // Free users with >80% of FreeQueryLimit used
                var highUsageUsers = paidUsers.Count(u =>
                {
                    var queryCount = queryHistory.Count(q => q.EmailId == u.LoginEmail);
                    return queryCount >= 0.8 * paidUserQueryLimit;
                });

                // Step 4: Return result
                return new PaidUsersOverviewDto
                {
                    TotalPaidUsers = paidUsers.Count(),
                    ActiveUsers = activeUsers,
                    InactiveUsers = inactiveUsers,
                    HighUsageUsers = highUsageUsers
                };
            }
        }
        private List<UserDetailsExcel> ReadExcel()
        {
            var users = new List<UserDetailsExcel>();

            using (var workbook = new XLWorkbook(_appsettings.UserFilePath))
            {
                var worksheet = workbook.Worksheet(1); // First sheet
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header row

                foreach (var row in rows)
                {
                    var user = new UserDetailsExcel
                    {
                        DisplayName = row.Cell(1).GetString(),
                        FirstName = row.Cell(2).GetString(),
                        LastName = row.Cell(3).GetString(),
                        LoginEmail = row.Cell(4).GetString(),
                        Phone = row.Cell(5).GetString(),
                        Courses = row.Cell(6).GetString(),
                        IsMembership = row.Cell(7).GetBoolean()
                    };

                    users.Add(user);
                }
            }

            return users;
        }

        public async Task<List<FreeUserQueryTypeDto>> GetFreeUserQueryTypesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                const string query = @"
           SELECT 
                LTRIM(RTRIM(q.Topic)) AS QueryType, 
                COUNT(*) AS Count
            FROM 
                QueryHistory q
            INNER JOIN 
                Users u ON q.UserId = u.Id
            WHERE 
                u.IsPremium = 0 
                AND q.Topic IS NOT NULL
                AND LTRIM(RTRIM(q.Topic)) <> ''
            GROUP BY 
                q.Topic
            ORDER BY 
                Count DESC;";

                try
                {
                    var result = await connection.QueryAsync<FreeUserQueryTypeDto>(query);
                    return result.ToList();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public async Task<List<UserDetail>> GetFreeUserDetailsAsync()
        {

            try
            {
                // Step 1: Read users from Excel
                var usersFromExcel = ReadExcel(); // List<UserDetails>

                using (var connection = new SqlConnection(_connectionString))
                {
                    // Step 2: Fetch sessions + query history from DB
                    var userSessions = (await connection.QueryAsync<UserSession>(
                        "SELECT  EmailId, LastActiveAt FROM UserSessions")).ToList();

                    var queryHistory = (await connection.QueryAsync<QueryHistoryDto>(
                        @"SELECT  * FROM QueryHistory q
                             CROSS APPLY OPENJSON(q.ChatJson)
                             WITH (
                                 QueryText NVARCHAR(MAX) '$.queryText',
                                 ResponseText NVARCHAR(MAX) '$.responseText',
                                 ResponseTime FLOAT '$.responseTime',
                                 Topic NVARCHAR(200) '$.topic',
                                 Status NVARCHAR(50) '$.status'
                             ) j
                             WHERE CAST(q.Timestamp AS DATE) = CAST(GETDATE() AS DATE);")).ToList();

                    var freeUserDetails = new List<UserDetail>();
                    // Step 3: Filter free users from Excel (IsMembership == false)
                    var freeUsers = usersFromExcel.Where(u => !u.IsMembership && string.IsNullOrEmpty(u.Courses)).ToList();
                    // Step 3: Work only on Free Users (IsMembership == false)
                    foreach (var user in freeUsers)
                    {
                        var email = user.LoginEmail;

                        // User sessions for this user
                        var sessions = userSessions.Where(s => s.EmailId == email).ToList();
                        var lastActive = sessions.Max(s => (DateTime?)s.LastActiveAt);

                        // Queries for this user
                        var userQueries = queryHistory.Where(q => q.EmailId == email).ToList();
                        var usedQueries = userQueries.Count;
                        var queryLimit = 10; // static limit (can fetch from SystemLimits if needed)

                        // Calculate status
                        string status = "Inactive";
                        if (lastActive.HasValue && lastActive.Value >= DateTime.Now.AddDays(-2))
                            status = "Active";

                        // Query usage in "x/10 (y%)" format
                        var queryUsage = $"{usedQueries}/{queryLimit} ({usedQueries * 10}%)";

                        // Calculate last activity in friendly format
                        string lastActivity = "";
                        if (lastActive.HasValue)
                        {
                            var diff = DateTime.Now - lastActive.Value;

                            if (diff.TotalMinutes < 60)
                                lastActivity = $"{(int)diff.TotalMinutes} min ago";
                            else if (diff.TotalHours < 24)
                                lastActivity = $"{(int)diff.TotalHours} hour ago";
                            else
                                lastActivity = $"{(int)diff.TotalDays} day ago";
                        }

                        // Build result
                        freeUserDetails.Add(new UserDetail
                        {
                            UserId = email, // since no DB Id, use Email as identifier
                            Status = status,
                            UsedQueries = usedQueries,
                            QueryLimit = queryLimit,
                            QueryUsage = queryUsage,
                            LastActivity = lastActivity
                        });
                    }

                    return freeUserDetails
                        .OrderByDescending(u => u.LastActivity) // similar to ORDER BY MAX(LastActiveAt) DESC
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching free user details: " + ex.Message);
            }
        }

        public async Task<List<UserDetail>> GetPaidUserDetailsAsync()
        {

            try
            {
                // Step 1: Read users from Excel
                var usersFromExcel = ReadExcel(); // List<UserDetails>

                using (var connection = new SqlConnection(_connectionString))
                {
                    // Step 2: Fetch sessions + query history from DB
                    var userSessions = (await connection.QueryAsync<UserSession>(
                        "SELECT  EmailId, LastActiveAt FROM UserSessions")).ToList();

                    var queryHistory = (await connection.QueryAsync<QueryHistoryDto>(
                        @"SELECT  * FROM QueryHistory q
                             CROSS APPLY OPENJSON(q.ChatJson)
                             WITH (
                                 QueryText NVARCHAR(MAX) '$.queryText',
                                 ResponseText NVARCHAR(MAX) '$.responseText',
                                 ResponseTime FLOAT '$.responseTime',
                                 Topic NVARCHAR(200) '$.topic',
                                 Status NVARCHAR(50) '$.status'
                             ) j
                             WHERE CAST(q.Timestamp AS DATE) = CAST(GETDATE() AS DATE);")).ToList();

                    var paidUserDetails = new List<UserDetail>();
                    // Step 3: Filter free users from Excel (IsMembership == false)
                    var freeUsers = usersFromExcel.Where(u => u.IsMembership || !string.IsNullOrEmpty(u.Courses)).ToList();
                    // Step 3: Work only on Free Users (IsMembership == false)
                    foreach (var user in freeUsers)
                    {
                        var email = user.LoginEmail;

                        // User sessions for this user
                        var sessions = userSessions.Where(s => s.EmailId == email).ToList();
                        var lastActive = sessions.Max(s => (DateTime?)s.LastActiveAt);

                        // Queries for this user
                        var userQueries = queryHistory.Where(q => q.EmailId == email).ToList();
                        var usedQueries = userQueries.Count;
                        var queryLimit = 10; // static limit (can fetch from SystemLimits if needed)

                        // Calculate status
                        string status = "Inactive";
                        if (lastActive.HasValue && lastActive.Value >= DateTime.Now.AddDays(-2))
                            status = "Active";

                        // Query usage in "x/10 (y%)" format
                        //var queryUsage = $"{usedQueries}/∞ ({usedQueries * 10}%)";
                        var queryUsage = $"{usedQueries}/∞";

                        // Calculate last activity in friendly format
                        string lastActivity = "";
                        if (lastActive.HasValue)
                        {
                            var diff = DateTime.Now - lastActive.Value;

                            if (diff.TotalMinutes < 60)
                                lastActivity = $"{(int)diff.TotalMinutes} min ago";
                            else if (diff.TotalHours < 24)
                                lastActivity = $"{(int)diff.TotalHours} hour ago";
                            else
                                lastActivity = $"{(int)diff.TotalDays} day ago";
                        }

                        // Build result
                        paidUserDetails.Add(new UserDetail
                        {
                            UserId = email, // since no DB Id, use Email as identifier
                            Status = status,
                            UsedQueries = usedQueries,
                            QueryLimit = queryLimit,
                            QueryUsage = queryUsage,
                            LastActivity = lastActivity
                        });
                    }

                    return paidUserDetails
                        .OrderByDescending(u => u.LastActivity) // similar to ORDER BY MAX(LastActiveAt) DESC
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching paid user details: " + ex.Message);
            }
        }

        public async Task<List<CommunicationSetting>> GetAllSettingsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT SettingId,UserType,EmailEnabled,SMSEnabled,WhatsAppEnabled FROM CommunicationSettings";
                var result = await connection.QueryAsync<CommunicationSetting>(query);
                return result.ToList();
            }
        }

        public async Task<int> UpdateSettingAsync(CommunicationSetting setting)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
                    UPDATE CommunicationSettings
                    SET 
                        EmailEnabled = @EmailEnabled,
                        SMSEnabled = @SMSEnabled,
                        WhatsAppEnabled = @WhatsAppEnabled,
                        UpdatedBy = @UpdatedBy,
                        UpdatedAt = @UpdatedAt
                    WHERE UserType = @UserType";

                return await connection.ExecuteAsync(query, setting);
            }
        }

    }
}

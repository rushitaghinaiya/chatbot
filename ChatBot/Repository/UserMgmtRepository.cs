using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Dapper;
using Microsoft.Data.SqlClient;
using static ChatBot.Models.Common.AesEncryptionHelper;

namespace ChatBot.Repository
{
    public class UserMgmtRepository : IUserMgmtService
    {
        private readonly string _connectionString;
        public UserMgmtRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<FreeUsersOverviewDto> GetFreeUsersOverviewAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {

                var query = @"
                SELECT 
                    -- Total Free Users
                    (SELECT COUNT(*) FROM Users WHERE IsPremium = 0) AS TotalFreeUsers,

                    -- Active Free Users (users having any session)
                    (SELECT COUNT(DISTINCT us.UserId)
                     FROM UserSessions us
                     INNER JOIN Users u ON us.UserId = u.Id
                     WHERE u.IsPremium = 0) AS ActiveUsers,

                    -- Inactive Free Users (free users who never had a session)
                    (SELECT COUNT(*)
                     FROM Users u
                     WHERE u.IsPremium = 0 AND u.Id NOT IN (
                         SELECT DISTINCT us.UserId FROM UserSessions us
                     )) AS InactiveUsers,

                    -- Free Users with >80% of their FreeQueryLimit used
                    (SELECT COUNT(*) 
                     FROM Users u
                     WHERE u.IsPremium = 0 AND 
                           (SELECT COUNT(*) FROM QueryHistory q WHERE q.UserId = u.Id) >= 0.8 * (select s.FreeUserQueryLimit from SystemLimits s)
                    ) AS HighUsageUsers";

                var result = await connection.QuerySingleAsync<FreeUsersOverviewDto>(query);
                return result;
            }
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

        public async Task<List<FreeUserDetail>> GetFreeUserDetailsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    string query = @"
                   SELECT 
                            u.Id AS UserId,
                            u.Mobile,
                            CASE 
                                WHEN MAX(us.LastActiveAt) >= DATEADD(DAY, -2, GETDATE()) THEN 'Active'
                                ELSE 'Inactive'
                            END AS Status,
                            COUNT(q.QueryId) AS UsedQueries,
                            10 AS QueryLimit,
                            CONCAT(COUNT(q.QueryId), '/10 (', CAST(COUNT(q.QueryId)*10 AS VARCHAR), '%)') AS QueryUsage,
                            CASE 
                                WHEN MAX(us.LastActiveAt) IS NULL THEN ''
                                WHEN DATEDIFF(MINUTE, MAX(us.LastActiveAt), GETDATE()) < 60 THEN 
                                    CONCAT(DATEDIFF(MINUTE, MAX(us.LastActiveAt), GETDATE()), ' min ago')
                                WHEN DATEDIFF(HOUR, MAX(us.LastActiveAt), GETDATE()) < 24 THEN 
                                    CONCAT(DATEDIFF(HOUR, MAX(us.LastActiveAt), GETDATE()), ' hour ago')
                                ELSE 
                                    CONCAT(DATEDIFF(DAY, MAX(us.LastActiveAt), GETDATE()), ' day ago')
                            END AS LastActivity
                        FROM Users u
                        LEFT JOIN QueryHistory q ON u.Id = q.UserId
                        LEFT JOIN UserSessions us ON u.Id = us.UserId
                        WHERE u.IsPremium = 0
                        GROUP BY u.Id, u.Mobile
                        ORDER BY MAX(us.LastActiveAt) DESC;";

                    var result = await connection.QueryAsync<FreeUserDetail>(query);
                    foreach (var item in result)
                    {
                        item.Mobile = Decrypt(item.Mobile);
                    }
                    return result.ToList();
                }
                catch (Exception ex)
                {
                    throw new Exception("Database error: " + ex.Message);
                }
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

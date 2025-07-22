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

        public async Task UpdateSessionAsync(int? userId, string sessionKey, string ip, string agent)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var query = @"
                MERGE INTO UserSessions AS target
                USING (SELECT @SessionKey AS SessionKey) AS source
                ON target.SessionId = source.SessionKey
                WHEN MATCHED THEN 
                    UPDATE SET LastActiveAt = GETDATE(), IPAddress = @IpAddress, UserAgent = @UserAgent
                WHEN NOT MATCHED THEN
                    INSERT (UserId, SessionId, LastActiveAt, IPAddress, UserAgent)
                    VALUES (@UserId, @SessionKey, GETDATE(), @IpAddress, @UserAgent);";

                    await connection.ExecuteAsync(query, new
                    {
                        UserId = userId,
                        SessionKey = sessionKey,
                        IpAddress = ip,
                        UserAgent = agent
                    });
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public List<UserSession> GetActiveSessions()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var sessions = connection.QueryAsync<UserSession>(
                        @"SELECT Id, UserId, SessionId, LastActiveAt, IPAddress, UserAgent
                  FROM UserSessions
                  WHERE LastActiveAt >= DATEADD(MINUTE, -10, GETDATE())
                  ORDER BY LastActiveAt DESC"
                    ).Result.ToList();

                    return sessions;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public UserStatsDto GetUserStats()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var totalUsers = connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users").Result;

                    var lastMonthCutoff = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    var usersUntilLastMonth = connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Users WHERE CreatedAt < @LastMonth",
                        new { LastMonth = lastMonthCutoff }
                    ).Result;

                    // Calculate % change
                    int newUsersThisMonth = totalUsers - usersUntilLastMonth;
                    double percentageChange = usersUntilLastMonth == 0
                        ? 100
                        : ((double)newUsersThisMonth / usersUntilLastMonth) * 100;

                    return new UserStatsDto
                    {
                        TotalUsers = totalUsers,
                        PercentageChange = Math.Round(percentageChange, 2)
                    };
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public ResponseTimeStatsDto GetAverageResponseTime()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var avgTime = connection.ExecuteScalarAsync<double?>(
                        @"SELECT AVG(CAST(ResponseTimeMs AS FLOAT)) 
                  FROM ChatbotQueries 
                  WHERE ResponseTimeMs IS NOT NULL"
                    ).Result;

                    return new ResponseTimeStatsDto
                    {
                        AverageResponseTimeMs = Math.Round(avgTime ?? 0, 2)
                    };
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public List<UserChatbotStatsDto> GetUserChatbotStats()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var result = connection.QueryAsync<UserChatbotStatsDto>(
                        @"SELECT 
                    CONCAT('U', RIGHT('000' + CAST(U.Id AS VARCHAR), 3)) AS UserId,
                    U.Mobile,
                    CASE WHEN U.IsPremium = 1 THEN 'Paid' ELSE 'Free' END AS Type,
                    CAST(COUNT(Q.Id) AS VARCHAR) + '/' +
                        CASE WHEN U.IsPremium = 1 THEN '∞' ELSE '10' END AS Queries,
                    0 AS Family,
                    CAST(SUM(ISNULL(Q.ResponseTimeMs, 0)) / 60000 AS INT) AS TimeInMin
                  FROM Users U
                  LEFT JOIN ChatbotQueries Q ON U.Id = Q.UserId
                  GROUP BY U.Id, U.Mobile, U.IsPremium
                  ORDER BY U.Id;"
                    ).Result.ToList();

                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public async Task<List<QueryTopicDistributionDto>> GetQueryTopicDistributionAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var queryCountsSql = @"
        SELECT QuestionGroup, COUNT(*) AS Count
        FROM ChatbotQueries
        GROUP BY QuestionGroup;
    ";

            var mappingSql = @"
        SELECT QuestionGroup, TopicName
        FROM QuestionGroupMapping;
    ";

            var groupCounts = (await connection.QueryAsync<(int QuestionGroup, int Count)>(queryCountsSql)).ToList();
            var mappings = (await connection.QueryAsync<(int QuestionGroup, string TopicName)>(mappingSql)).ToDictionary(x => x.QuestionGroup, x => x.TopicName);

            int total = groupCounts.Sum(x => x.Count);

            var distribution = groupCounts
                .Where(x => mappings.ContainsKey(x.QuestionGroup))
                .Select(x => new QueryTopicDistributionDto
                {
                    TopicName = mappings[x.QuestionGroup],
                    Percentage = Math.Round((x.Count * 100.0) / total, 2)
                })
                .OrderByDescending(x => x.Percentage)
                .ToList();

            return distribution;
        }

        public async Task<List<QueryStatusDistribution>> GetQueryStatusDistributionAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT Status, COUNT(*) AS Count 
                      FROM ChatbotQueries 
                      GROUP BY Status";

                var result = await connection.QueryAsync<QueryStatusDistribution>(query);
                return result.ToList();
            }
        }

        public async Task<List<UserTypeDistribution>> GetUserTypeDistributionAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
            SELECT 
                CASE 
                    WHEN IsPremium = 1 THEN 'Paid Users'
                    ELSE 'Free Users'
                END AS UserType,
                COUNT(*) AS Count,
                ROUND(100.0 * COUNT(*) / (SELECT COUNT(*) FROM Users), 2) AS Percentage
            FROM Users
            GROUP BY IsPremium";

                var result = await connection.QueryAsync<UserTypeDistribution>(query);
                return result.ToList();
            }
        }

        public async Task<AverageMetricsDto> GetAverageMetricsAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var query = @"
        SELECT
            ROUND(AVG(CAST(DurationInSeconds AS FLOAT)) / 60, 1) AS AvgDuration,
            ROUND(CAST(COUNT(Q.Id) AS FLOAT) / NULLIF(COUNT(DISTINCT U.Id), 0), 1) AS AvgQueries,
            ROUND(CAST(COUNT(F.Id) AS FLOAT) / NULLIF(COUNT(DISTINCT U.Id), 0), 1) AS AvgFamily
        FROM Users U
        LEFT JOIN Sessions S ON S.UserId = U.Id
        LEFT JOIN Queries Q ON Q.UserId = U.Id
        LEFT JOIN FamilyUsers F ON F.UserId = U.Id;
    ";

            var result = await connection.QueryFirstOrDefaultAsync(query);

            return new AverageMetricsDto
            {
                AvgSessionDuration = $"{result.AvgDuration} min",
                AvgQueriesPerUser = result.AvgQueries,
                AvgFamilyMembersPerUser = result.AvgFamily
            };
        }

        public async Task<List<AdminLoginLog>> GetAdminLogsAndStatusAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT * FROM AdminLoginLogs";

                var result = await connection.QueryAsync<AdminLoginLog>(query);
                return result.ToList();
            }
        }
    }
}
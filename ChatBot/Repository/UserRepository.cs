using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static ChatBot.Models.Common.AesEncryptionHelper;

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
                        "SELECT * FROM users u INNER JOIN refreshtoken  rt ON u.userid= rt.userId WHERE rt.Token=@Token ;", param: new { Token = token }
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

        public async Task<(int todayCount, int lastMonthCount, double percentageChange)> GetTodayQueryStatsAsync()
        {
            var today = DateTime.Today;
            var lastMonthStart = today.AddMonths(-1);
            var lastMonthEnd = today.AddDays(-1); // yesterday

            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
            SELECT 
                COUNT(CASE WHEN CAST(Timestamp AS DATE) = @Today THEN 1 END) AS TodayCount,
                COUNT(CASE WHEN CAST(Timestamp AS DATE) BETWEEN @LastMonthStart AND @LastMonthEnd THEN 1 END) AS LastMonthCount
            FROM QueryHistory";

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    Today = today,
                    LastMonthStart = lastMonthStart,
                    LastMonthEnd = lastMonthEnd
                });

                int todayCount = result.TodayCount;
                int lastMonthCount = result.LastMonthCount;

                double percentChange = 0;
                if (lastMonthCount > 0)
                {
                    percentChange = Math.Round(((todayCount - lastMonthCount) / (double)lastMonthCount) * 100, 2);
                }
                

                return (todayCount, lastMonthCount, percentChange);
            }
        }

        public async Task<(double avgResponseTime, double lastMonthAvg, double percentageChange)> GetAverageResponseTimeAsync()
        {
            var today = DateTime.Today;
            var lastMonthStart = today.AddMonths(-1);
            var lastMonthEnd = today.AddDays(-1);

            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
            SELECT
                AVG(CASE 
                    WHEN ResponseTime > 0 AND CAST(Timestamp AS DATE) BETWEEN @LastMonthStart AND @LastMonthEnd 
                    THEN ResponseTime 
                END) AS LastMonthAvg,
                
                AVG(CASE 
                    WHEN ResponseTime > 0 AND CAST(Timestamp AS DATE) = @Today 
                    THEN ResponseTime 
                END) AS TodayAvg
            FROM QueryHistory";

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    Today = today,
                    LastMonthStart = lastMonthStart,
                    LastMonthEnd = lastMonthEnd
                });

                double todayAvg = Math.Round((double)(result?.TodayAvg ?? 0m), 2);
                double lastMonthAvg = Math.Round((double)(result?.LastMonthAvg ?? 0m), 2);

                double percentChange = 0;
                if (lastMonthAvg > 0)
                {
                    percentChange = Math.Round(((todayAvg - lastMonthAvg) / lastMonthAvg) * 100, 2);
                }

                return (todayAvg, lastMonthAvg, percentChange);
            }
        }

        public async Task<SessionStatsDto> GetSessionStats()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var today = DateTime.Today;

                    // Current period: 25th of previous month to 24th of this month
                    var currentStart = new DateTime(today.Year, today.Month, 25).AddMonths(-1);
                    var currentEnd = new DateTime(today.Year, today.Month, 24);

                    // Last month period: one month before current
                    var lastStart = currentStart.AddMonths(-1);
                    var lastEnd = currentEnd.AddMonths(-1);

                    string sql = @"
                            SELECT COUNT(DISTINCT UserId) 
                            FROM UserSessions 
                            WHERE LastActiveAt BETWEEN @Start AND @End";

                    int currentUserCount = connection.ExecuteScalar<int>(sql, new { Start = currentStart, End = currentEnd });
                    int lastUserCount = connection.ExecuteScalar<int>(sql, new { Start = lastStart, End = lastEnd });

                    double percentageChange = lastUserCount == 0
                        ? 100
                        : ((double)(currentUserCount - lastUserCount) / lastUserCount) * 100;

                    return new SessionStatsDto
                    {
                        TotalSessions = currentUserCount,
                        PercentageChange = Math.Round(percentageChange, 2)
                    };
                }
                catch
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
                    CAST(COUNT(Q.QueryId) AS VARCHAR) + '/' +
                        CASE WHEN U.IsPremium = 1 THEN '∞' ELSE '10' END AS Queries,
                    0 AS Family,
                    CAST(SUM(ISNULL(Q.ResponseTime, 0)) / 60000 AS INT) AS TimeInMin
                  FROM Users U
                  LEFT JOIN queryhistory Q ON U.Id = Q.UserId
                  GROUP BY U.Id, U.Mobile, U.IsPremium
                  ORDER BY U.Id;"
                    ).Result.ToList();

                    foreach (var item in result)
                    {
                        item.Mobile = Decrypt(item.Mobile);
                    }

                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public async Task<List<QueryTopicDistributionDto>> GetQueryTopicDistribution()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var topicCounts = connection.Query<QueryTopicDistributionDto>(
                    @"SELECT 
                    Topic, 
                    COUNT(*) AS QueryCount
                  FROM QueryHistory
                  WHERE  ISNULL(LTRIM(RTRIM(Topic)), '') <> ''
                  GROUP BY Topic
                  ORDER BY QueryCount DESC"
                ).ToList();

                int total = topicCounts.Sum(q => q.QueryCount);

                foreach (var topic in topicCounts)
                {
                    topic.Percentage = total == 0 ? 0 : Math.Round((double)topic.QueryCount * 100 / total, 2);
                }

                return topicCounts;
            }
        }


        public async Task<QueryStatusDistribution> GetQueryStatusDistributionAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"
                SELECT 
                    SUM(CASE WHEN Status = 'Answered' THEN 1 ELSE 0 END) AS AnsweredCount,
                    SUM(CASE WHEN Status = 'Unanswered' THEN 1 ELSE 0 END) AS UnansweredCount,
                    SUM(CASE WHEN Status = 'Incomplete' THEN 1 ELSE 0 END) AS IncompleteCount
                FROM QueryHistory";

                return connection.QuerySingle<QueryStatusDistribution>(sql);
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

            var AvgDuration = await connection.QueryFirstOrDefaultAsync<double?>(@"
                SELECT 
                ROUND(AVG(CAST(S.totalTimeSpent AS FLOAT)) / 60, 1) AS AvgDuration
                FROM BotSessions S
                WHERE CAST(S.StartTime AS DATE) = CAST(GETDATE() AS DATE)
            ");

            var AvgQueries = await connection.QueryFirstOrDefaultAsync<double?>(@"
                SELECT 
                ROUND(CAST(COUNT(Q.QueryId) AS FLOAT) / NULLIF(COUNT(DISTINCT Q.UserId), 0), 1) AS AvgQueries
                FROM QueryHistory Q
                WHERE CAST(Q.Timestamp AS DATE) = CAST(GETDATE() AS DATE)
            ");


            return new AverageMetricsDto
            {
                AvgSessionDuration = $"{AvgDuration} min",
                AvgQueriesPerUser = Convert.ToDouble(AvgQueries),
                AvgFamilyMembersPerUser = 0
            };
        }

        public async Task<List<AdminLoginLog>> GetAdminLogsAndStatusAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT 
                        a.AdminId ,
                        a.Email,
                        MAX(l.LoginTime) AS LastActivityTime,
                        STUFF((
                            SELECT DISTINCT ', ' + innerLog.Actions
                            FROM [ChatbotDB].[dbo].[AdminLoginLogs] AS innerLog
                            WHERE innerLog.AdminId = a.AdminId
                              AND innerLog.LoginTime >= DATEADD(DAY, -30, GETDATE())
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS Actions
                    FROM 
                        [ChatbotDB].[dbo].[Admins] a
                    JOIN 
                        [ChatbotDB].[dbo].[AdminLoginLogs] l ON a.AdminId = l.AdminId
                    WHERE 
                        l.LoginTime >= DATEADD(DAY, -30, GETDATE())
                    GROUP BY 
                        a.AdminId, a.Email;";

                var result = await connection.QueryAsync<AdminLoginLog>(query);
                return result.ToList();
            }
        }

        public async Task<bool> SaveUserSession(BotSession botSession)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var sessionId = await connection.QuerySingleAsync<int>(@"
                    INSERT INTO BotSessions(UserId, StartTime, EndTime,TotalTimeSpent, CreatedAt)
                    VALUES(@user_id, @start_time, @end_time,@totalTimeSpent, @created_at);
                    SELECT CAST(SCOPE_IDENTITY() as int);",
                            new
                            {
                                user_id = botSession.UserId,
                                start_time = botSession.StartTime,
                                end_time = botSession.EndTime,
                                created_at = DateTime.Now,
                                totalTimeSpent = botSession.TotalTimeSpent,
                            }, transaction: transaction);

                        transaction.Commit();
                        return sessionId > 0;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public async Task<bool> SaveQueryHistory(QueryHistoryDto dto)
        {
            var query = new
            {
                dto.UserId,
                dto.QueryText,
                dto.ResponseText,
                dto.ResponseTime,
                dto.Topic,
                Timestamp = DateTime.Now,
                dto.Status
            };
            try
            {
                var sql = @"INSERT INTO queryhistory 
                ( UserId, QueryText, ResponseText,  ResponseTime, topic, timestamp, status)
                VALUES 
                (@UserId, @QueryText, @ResponseText, @ResponseTime, @Topic, @Timestamp, @Status);
                 SELECT CAST(SCOPE_IDENTITY() as int);";

                using (var connection = new SqlConnection(_connectionString))
                {
                    var queryId = await connection.QuerySingleAsync<int>(sql, query);
                    return queryId > 0;
                }

            }
            catch (Exception)
            {
                return false;
            }
        }


    }
}
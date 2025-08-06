using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using static ChatBot.Models.Common.AesEncryptionHelper;
using Dapper;
using Model.ViewModels;
using Microsoft.Data.SqlClient;
using System.Data;
using VRMDBCommon2023;
using ChatBot.Models.Entities;

namespace ChatBot.Repository
{
    public class ApiLogRepository : IApiLogService
    {
        private string _connectionString;
        public ApiLogRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

      
        public async Task LogAsync(ApiLog log)
        {
            var query = @"
            INSERT INTO ApiLogs (ApiName, UserId, RequestTime, RequestMethod, RequestHeaders, RequestBody, QueryString)
            VALUES (@ApiName, @UserId, @RequestTime, @RequestMethod, @RequestHeaders, @RequestBody, @QueryString);
        ";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                await connection.ExecuteAsync(query, log);
            }
        }
    }
}

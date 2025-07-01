using ChatBot.Models.Entities;
using ChatBot.Models.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ChatBot.Repository
{
    public class ExceptionLogRepository : IExceptionLog
    {
        private readonly string _connectionString;
        public ExceptionLogRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Log(ExceptionLog log)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    var insertedId = connection.ExecuteAsync(@"
                        INSERT INTO ExceptionLogs (Message, StackTrace, ExceptionType, Path, Method, StatusCode, Timestamp, [User]) 
                        VALUES (@Message, @StackTrace, @ExceptionType, @Path, @Method, @StatusCode, @Timestamp, @User);",
                        log, transaction: transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
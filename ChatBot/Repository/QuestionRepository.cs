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
    public class QuestionRepository : IQuestion
    {
        private readonly string _connectionString;
        public QuestionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Question> GetQuestionGroup()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var questions = connection.QueryAsync<Question>(
                        "SELECT Id, Text, Category, IsActive, CreatedAt, UpdatedAt FROM QuestionGroup"
                    ).Result.ToList();

                    return questions;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public List<Question> GetQuestionsById(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var questions = connection.QueryAsync<Question>(
                        "SELECT Id, GroupId, Text, Category, IsActive, CreatedAt, UpdatedAt FROM Questions WHERE GroupId = @id",
                        param: new { id }).Result.ToList();

                    return questions;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public List<Question> GetQuestionsList()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var questions = connection.QueryAsync<Question>(
                        "SELECT Id, GroupId, Text,Value, Category, IsActive, CreatedAt, UpdatedAt FROM Questions WHERE IsActive = 1").Result.ToList();

                    return questions;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
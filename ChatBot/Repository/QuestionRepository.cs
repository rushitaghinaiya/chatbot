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
    public class QuestionRepository : IQuestion
    {
        private readonly string _connectionString;
        public QuestionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public List<Question> GetQuestionGroup()
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    var questions = connection.QueryAsync<Question>(
                        "SELECT id, text, category, is_active, created_at, updated_at FROM question_group"
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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    var questions = connection.QueryAsync<Question>(
                        "SELECT id, groupid, text, category, is_active, created_at, updated_at FROM questions where groupid=@id",
                        param: new { id }).Result.ToList();

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

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
    public class AdminRepository : IAdmin
    {
        private readonly string _connectionString;
        public AdminRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int SaveFileMetadataToDatabase(UploadFile file)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                MySqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    var insertedId = connection.QueryAsync<int>(@"
                INSERT INTO uploaded_files 
                      (uploaded_by, filename, filetype, filesize, status, queries, created_at, edited_at)
                      VALUES 
                      (@UploadedBy, @FileName, @FileType, @FileSize, @Status, @Queries, @CreatedAt, @EditedAt);
      
                SELECT LAST_INSERT_ID();",
                        new
                        {
                            file.UploadedBy,
                            file.FileName,
                            file.FileType,
                            FileSize=10,
                            file.Status,
                            file.Queries,
                            file.CreatedAt,
                            file.EditedAt
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
    }
}


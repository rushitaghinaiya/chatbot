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
    public class AdminRepository : IAdmin
    {
        private readonly string _connectionString;
        public AdminRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int SaveFileMetadataToDatabase(UploadFile file)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    var insertedId = connection.QueryAsync<int>(@"
                        INSERT INTO UploadedFiles 
                        (UploadedBy, FileName, FileType, FileSize, Status, Queries, CreatedAt, EditedAt)
                        VALUES 
                        (@UploadedBy, @FileName, @FileType, @FileSize, @Status, @Queries, @CreatedAt, @EditedAt);
                        SELECT CAST(SCOPE_IDENTITY() as int);",
                        new
                        {
                            file.UploadedBy,
                            file.FileName,
                            file.FileType,
                            FileSize = file.FileSize ?? 10,
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
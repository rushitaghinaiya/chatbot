using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using static ChatBot.Models.Common.AesEncryptionHelper;
using Dapper;
using Model.ViewModels;
using Microsoft.Data.SqlClient;
using System.Data;
using VRMDBCommon2023;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ChatBot.Repository
{
    public class SettingRepository : ISetting
    {
        private readonly string _connectionString;
        public SettingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<List<Language>> GetLanguage()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var questions = connection.QueryAsync<Language>(
                        "SELECT Id, languageName,language_code ,IsActive FROM language"
                    ).Result.ToList();

                    return questions;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public SecuritySettings? GetSecuritySettings()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var setting = connection.QueryFirstOrDefaultAsync<SecuritySettings>(
                        "SELECT * FROM SecuritySettings"
                    ).Result;

                    return setting;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public bool UpdateSecuritySettings(SecuritySettings model)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    model.UpdatedOn = DateTime.UtcNow;

                    var rows = connection.ExecuteAsync(@"
                    UPDATE SecuritySettings SET 
                        TwoFactorAuthEnabled = @TwoFactorAuthEnabled,
                        AutoLogoutEnabled = @AutoLogoutEnabled,
                        AutoLogoutDurationMinutes = @AutoLogoutDurationMinutes,
                        EnableAuditLogging = @EnableAuditLogging,
                        UpdatedBy = @UpdatedBy,
                        UpdatedOn = @UpdatedOn
                    WHERE Id = @Id
                ", model).Result;

                    return rows > 0;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public VoiceAccessibilitySettings? GetVoiceSettings()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var setting = connection.QueryFirstOrDefaultAsync<VoiceAccessibilitySettings>(
                        "SELECT * FROM VoiceAccessibilitySettings"
                    ).Result;

                    return setting;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public bool UpdateVoiceSettings(VoiceAccessibilitySettings model)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    model.UpdatedOn = DateTime.UtcNow;

                    var rows = connection.ExecuteAsync(@"
                    UPDATE VoiceAccessibilitySettings SET 
                        EnableVoiceCommands = @EnableVoiceCommands,
                        EnableTextToSpeech = @EnableTextToSpeech,
                        EnableVoiceToText = @EnableVoiceToText,
                        UpdatedBy = @UpdatedBy,
                        UpdatedOn = @UpdatedOn
                    WHERE Id = @Id
                ", model).Result;

                    return rows > 0;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public SystemLimits? GetSystemLimits()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var setting = connection.QueryFirstOrDefaultAsync<SystemLimits>(
                        "SELECT * FROM SystemLimits"
                    ).Result;

                    return setting;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public bool UpdateSystemLimits(SystemLimits model)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    model.UpdatedOn = DateTime.UtcNow;

                    var rows = connection.ExecuteAsync(@"
                    UPDATE SystemLimits SET 
                        FreeUserQueryLimit = @FreeUserQueryLimit,
                        CharacterLimitPerQuery = @CharacterLimitPerQuery,
                        UpdatedBy = @UpdatedBy,
                        UpdatedOn = @UpdatedOn
                    WHERE Id = @Id
                ", model).Result;

                    return rows > 0;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }

}


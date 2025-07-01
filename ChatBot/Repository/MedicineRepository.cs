using ChatBot.Models.ViewModels;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ChatBot.Models.Services;

namespace ChatBot.Repository
{
    public class MedicineRepository : IMedicine
    {
        private readonly string _connectionString;

        public MedicineRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<SearchResult<MedicineSearchVM>> SearchMedicinesAsync(string name, int page, int pageSize, bool includeDiscontinued)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                try
                {
                    var offset = (page - 1) * pageSize;
                    var whereClause = includeDiscontinued ? "" : "AND (IsDiscontinued = 0 OR IsDiscontinued IS NULL)";

                    var searchWords = name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    var searchLike = "%" + string.Join("%", searchWords) + "%";
                    var exactName = name + "%";

                    var countSql = $@"
                        SELECT COUNT(*) 
                        FROM MedicineData 
                        WHERE LOWER(Name) LIKE @SearchName 
                        {whereClause}";

                    var sql = $@"
                        SELECT 
                            Id AS Id,
                            Name AS Name,
                            Price AS Price,
                            IsDiscontinued AS IsDiscontinued,
                            ManufacturerName AS ManufacturerName,
                            Type AS Type,
                            PackSizeLabel AS PackSizeLabel,
                            ShortComposition1 AS ShortComposition1,
                            ShortComposition2 AS ShortComposition2,
                            SaltComposition AS SaltComposition,
                            MedicineDesc AS MedicineDescription,
                            SideEffects AS SideEffects,
                            DrugInteractions AS DrugInteractions
                        FROM MedicineData 
                        WHERE LOWER(Name) LIKE @SearchName 
                        {whereClause}
                        ORDER BY 
                            CASE 
                                WHEN LOWER(Name) LIKE @ExactName THEN 1 
                                ELSE 2 
                            END,
                            Name
                        OFFSET @Offset ROWS 
                        FETCH NEXT @PageSize ROWS ONLY";

                    var totalCount = await connection.QuerySingleAsync<int>(countSql, new
                    {
                        SearchName = searchLike
                    });

                    var medicines = await connection.QueryAsync<MedicineSearchVM>(sql, new
                    {
                        SearchName = searchLike,
                        ExactName = exactName,
                        PageSize = pageSize,
                        Offset = offset
                    });

                    return new SearchResult<MedicineSearchVM>
                    {
                        Items = medicines.ToList(),
                        TotalCount = totalCount
                    };
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public async Task<MedicineSearchVM?> GetMedicineByIdAsync(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                try
                {
                    var sql = @"
                        SELECT 
                            Id as Id,
                            Name as Name,
                            Price as Price,
                            IsDiscontinued as IsDiscontinued,
                            ManufacturerName as ManufacturerName,
                            Type as Type,
                            PackSizeLabel as PackSizeLabel,
                            ShortComposition1 as ShortComposition1,
                            ShortComposition2 as ShortComposition2,
                            SaltComposition as SaltComposition,
                            MedicineDesc as MedicineDescription,
                            SideEffects as SideEffects,
                            DrugInteractions as DrugInteractions
                        FROM MedicineData 
                        WHERE Id = @Id";

                    return await connection.QuerySingleOrDefaultAsync<MedicineSearchVM>(sql, new { Id = id });
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public async Task<List<MedicineSearchVM>> GetMedicinesByExactNameAsync(string name)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                try
                {
                    var sql = @"
                        SELECT 
                            Id as Id,
                            Name as Name,
                            Price as Price,
                            IsDiscontinued as IsDiscontinued,
                            ManufacturerName as ManufacturerName,
                            Type as Type,
                            PackSizeLabel as PackSizeLabel,
                            ShortComposition1 as ShortComposition1,
                            ShortComposition2 as ShortComposition2,
                            SaltComposition as SaltComposition,
                            MedicineDesc as MedicineDescription,
                            SideEffects as SideEffects,
                            DrugInteractions as DrugInteractions
                        FROM MedicineData 
                        WHERE Name = @Name 
                        AND (IsDiscontinued = 0 OR IsDiscontinued IS NULL)
                        ORDER BY Name";

                    var medicines = await connection.QueryAsync<MedicineSearchVM>(sql, new { Name = name });
                    return medicines.ToList();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
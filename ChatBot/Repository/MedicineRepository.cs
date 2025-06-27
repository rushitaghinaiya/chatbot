using ChatBot.Models.ViewModels;
using System.Data;
using Dapper;
using MySqlConnector;
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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                try
                {
                    var offset = (page - 1) * pageSize;
                    var whereClause = includeDiscontinued ? "" : "AND (Is_discontinued = 0 OR Is_discontinued IS NULL)";


                    var searchWords = name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    var searchLike = "%" + string.Join("%", searchWords) + "%";
                    var exactName = name + "%";

                    var countSql = $@"
                SELECT COUNT(*) 
                FROM medicinedata 
                WHERE LOWER(name) LIKE @SearchName 
                {whereClause}";

                    var sql = $@"
                SELECT 
                    id AS Id,
                    name AS Name,
                    price AS Price,
                    Is_discontinued AS IsDiscontinued,
                    manufacturer_name AS ManufacturerName,
                    type AS Type,
                    pack_size_label AS PackSizeLabel,
                    short_composition1 AS ShortComposition1,
                    short_composition2 AS ShortComposition2,
                    salt_composition AS SaltComposition,
                    medicine_desc AS MedicineDescription,
                    side_effects AS SideEffects,
                    drug_interactions AS DrugInteractions
                FROM medicinedata 
                WHERE LOWER(name) LIKE @SearchName 
                {whereClause}
                ORDER BY 
                    CASE 
                        WHEN LOWER(name) LIKE @ExactName THEN 1 
                        ELSE 2 
                    END,
                    name
                LIMIT @PageSize OFFSET @Offset";

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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                try
                {
                    var sql = @"
                        SELECT 
                            id as Id,
                            name as Name,
                            price as Price,
                            Is_discontinued as IsDiscontinued,
                            manufacturer_name as ManufacturerName,
                            type as Type,
                            pack_size_label as PackSizeLabel,
                            short_composition1 as ShortComposition1,
                            short_composition2 as ShortComposition2,
                            salt_composition as SaltComposition,
                            medicine_desc as MedicineDescription,
                            side_effects as SideEffects,
                            drug_interactions as DrugInteractions
                        FROM medicinedata 
                        WHERE id = @Id";

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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                try
                {
                    var sql = @"
                        SELECT 
                            id as Id,
                            name as Name,
                            price as Price,
                            Is_discontinued as IsDiscontinued,
                            manufacturer_name as ManufacturerName,
                            type as Type,
                            pack_size_label as PackSizeLabel,
                            short_composition1 as ShortComposition1,
                            short_composition2 as ShortComposition2,
                            salt_composition as SaltComposition,
                            medicine_desc as MedicineDescription,
                            side_effects as SideEffects,
                            drug_interactions as DrugInteractions
                        FROM medicinedata 
                        WHERE name = @Name 
                        AND (Is_discontinued = 0 OR Is_discontinued IS NULL)
                        ORDER BY name";

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
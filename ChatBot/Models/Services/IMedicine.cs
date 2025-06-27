using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IMedicine
    {
        Task<SearchResult<MedicineSearchVM>> SearchMedicinesAsync(string name, int page, int pageSize, bool includeDiscontinued);
        Task<MedicineSearchVM?> GetMedicineByIdAsync(int id);
        Task<List<MedicineSearchVM>> GetMedicinesByExactNameAsync(string name);
    }
}

using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using ChatBot.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicinesController : ControllerBase
    {
        private readonly IMedicine _medicine;
        private readonly ILogger<MedicinesController> _logger;

        public MedicinesController(IMedicine medicine, ILogger<MedicinesController> logger)
        {
            _medicine = medicine;
            _logger = logger;
        }

        // GET: api/medicines/search?name=paracetamol&page=1&pageSize=10
        [HttpGet("search")]
        public async Task<ActionResult<List<MedicineSearchVM>>> SearchMedicines(
            [FromQuery] string name,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeDiscontinued = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return StatusCode(400,"Medicine name is required for search");
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _medicine.SearchMedicinesAsync(name.Trim(), page, pageSize, includeDiscontinued);

                return Ok(result.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching medicines with name: {Name}", name);
                return StatusCode(500,"An error occurred while searching medicines");
            }
        }

        // GET: api/medicines/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MedicineSearchVM>> GetMedicineById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return StatusCode(400, "Invalid medicine ID");
                }

                var medicine = await _medicine.GetMedicineByIdAsync(id);

                if (medicine == null)
                {
                    return StatusCode(404,"Medicine not found");
                }

                return Ok(medicine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting medicine with ID: {Id}", id);
                return StatusCode(500,"An error occurred while retrieving medicine");
            }
        }

        // GET: api/medicines/exact?name=Paracetamol
        [HttpGet("exact")]
        public async Task<ActionResult<List<MedicineSearchVM>>> GetMedicinesByExactName(
            [FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return StatusCode(400,"Medicine name is required");
                }

                var medicines = await _medicine.GetMedicinesByExactNameAsync(name.Trim());

                return Ok(medicines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting medicines with exact name: {Name}", name);
                return StatusCode(500,"An error occurred while retrieving medicines");
            }
        }
    }
}
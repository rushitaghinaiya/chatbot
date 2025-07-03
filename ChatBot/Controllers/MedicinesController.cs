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

        /// <summary>
        /// Searches medicines by name with pagination and optional discontinued filter.
        /// </summary>
        /// <param name="name">Medicine name to search for.</param>
        /// <param name="page">Page number for pagination.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="includeDiscontinued">Include discontinued medicines.</param>
        /// <returns>List of matching medicines.</returns>
        // GET: api/medicines/search?name=paracetamol&page=1&pageSize=10
        [HttpGet("search")]
        public async Task<ActionResult<List<MedicineSearchVM>>> SearchMedicines(
            [FromQuery] string name,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeDiscontinued = false)
        {
            _logger.LogInformation("Searching medicines with name: {Name}, page: {Page}, pageSize: {PageSize}, includeDiscontinued: {IncludeDiscontinued}", name, page, pageSize, includeDiscontinued);

            if (string.IsNullOrWhiteSpace(name))
            {
                return StatusCode(400, "Medicine name is required for search");
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _medicine.SearchMedicinesAsync(name.Trim(), page, pageSize, includeDiscontinued);

            return Ok(result.Items);
        }

        /// <summary>
        /// Gets a medicine by its unique ID.
        /// </summary>
        /// <param name="id">Medicine ID.</param>
        /// <returns>Medicine details if found.</returns>
        // GET: api/medicines/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MedicineSearchVM>> GetMedicineById(int id)
        {
            _logger.LogInformation("Getting medicine with ID: {Id}", id);

            if (id <= 0)
            {
                return StatusCode(400, "Invalid medicine ID");
            }

            var medicine = await _medicine.GetMedicineByIdAsync(id);

            if (medicine == null)
            {
                return StatusCode(404, "Medicine not found");
            }

            return Ok(medicine);
        }

        /// <summary>
        /// Gets medicines by exact name match.
        /// </summary>
        /// <param name="name">Exact medicine name.</param>
        /// <returns>List of medicines with the exact name.</returns>
        // GET: api/medicines/exact?name=Paracetamol
        [HttpGet("exact")]
        public async Task<ActionResult<List<MedicineSearchVM>>> GetMedicinesByExactName(
            [FromQuery] string name)
        {
            _logger.LogInformation("Getting medicines with exact name: {Name}", name);

            if (string.IsNullOrWhiteSpace(name))
            {
                return StatusCode(400, "Medicine name is required");
            }

            var medicines = await _medicine.GetMedicinesByExactNameAsync(name.Trim());

            return Ok(medicines);
        }
    }
}
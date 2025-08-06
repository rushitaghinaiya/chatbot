using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using ChatBot.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers
{
    [Route("v1/[controller]")]
    [ApiController] 
    [Authorize] // All endpoints in this controller require JWT authentication
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
        /// Searches medicines by name with pagination. Requires JWT authentication.
        /// Users can only search medicines if they are authenticated.
        /// </summary>
        /// <param name="name">Medicine name to search for.</param>
        /// <param name="page">Page number for pagination.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="includeDiscontinued">Include discontinued medicines.</param>
        /// <returns>List of matching medicines.</returns>
        [HttpGet("search")]
        public async Task<ActionResult<List<MedicineSearchVM>>> SearchMedicines(
            [FromQuery] string name,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeDiscontinued = false)
        {
            // Get current user information from JWT token
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst("role")?.Value;
            var isPremium = User.FindFirst("isPremium")?.Value == "True";

            _logger.LogInformation("Medicine search by user: {UserId}, Role: {Role}, Premium: {IsPremium}",
                currentUserId, userRole, isPremium);

            if (string.IsNullOrWhiteSpace(name))
            {
                return StatusCode(400, "Medicine name is required for search");
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            // Limit search results for non-premium users
            if (!isPremium && pageSize > 10)
            {
                pageSize = 10; // Free users limited to 10 results
            }

            var result = await _medicine.SearchMedicinesAsync(name.Trim(), page, pageSize, includeDiscontinued);

            // For non-premium users, you might want to limit the information returned
            if (!isPremium)
            {
                foreach (var medicine in result.Items)
                {
                    medicine.Price = null; // Hide pricing for free users
                    medicine.SideEffects = null; // Hide detailed side effects for free users
                }
            }

            return Ok(result.Items);
        }

        /// <summary>
        /// Gets detailed medicine information. Requires premium subscription.
        /// </summary>
        /// <param name="id">Medicine ID.</param>
        /// <returns>Detailed medicine information.</returns>
        [HttpGet("detailed/{id}")]
        public async Task<ActionResult<MedicineSearchVM>> GetDetailedMedicineInfo(int id)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isPremium = User.FindFirst("isPremium")?.Value == "True";

            _logger.LogInformation("Detailed medicine info request by user: {UserId}, Premium: {IsPremium}",
                currentUserId, isPremium);

            // Check if user has premium access for detailed information
            if (!isPremium)
            {
                return StatusCode(403, new
                {
                    Success = false,
                    Message = "Premium subscription required for detailed medicine information",
                    ErrorCode = "PREMIUM_REQUIRED"
                });
            }

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
        /// Admin-only endpoint to get medicine statistics.
        /// </summary>
        /// <returns>Medicine database statistics.</returns>
        [HttpGet("admin/statistics")]
        public async Task<ActionResult<object>> GetMedicineStatistics()
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst("role")?.Value;

            _logger.LogInformation("Medicine statistics request by user: {UserId}, Role: {Role}",
                currentUserId, userRole);

            // Check if user has admin privileges
            if (userRole != "admin")
            {
                return StatusCode(403, new
                {
                    Success = false,
                    Message = "Access denied. Admin privileges required.",
                    ErrorCode = "INSUFFICIENT_PRIVILEGES"
                });
            }

            // Return statistics (you would implement actual statistics gathering)
            var statistics = new
            {
                TotalMedicines = 0, // Implement actual counting
                DiscontinuedMedicines = 0,
                LastUpdated = DateTime.UtcNow,
                PopularSearches = new string[] { "Paracetamol", "Aspirin", "Ibuprofen" }, // Example data
                RequestedBy = currentUserId,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(statistics);
        }

        /// <summary>
        /// Public endpoint that doesn't require authentication.
        /// </summary>
        /// <returns>Basic API information.</returns>
        [HttpGet("public/info")]
        [AllowAnonymous] // This overrides the controller-level [Authorize] attribute
        public IActionResult GetPublicInfo()
        {
            _logger.LogInformation("Public medicine API info accessed");

            return Ok(new
            {
                Service = "Medicine API",
                Version = "1.0.0",
                Status = "Active",
                Features = new[]
                {
                    "Medicine Search",
                    "Detailed Information (Premium)",
                    "Admin Statistics"
                },
                Authentication = "JWT Bearer Token Required (except this endpoint)",
                Timestamp = DateTime.UtcNow
            });
        }
    }

   
}
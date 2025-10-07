using Microsoft.AspNetCore.Mvc;
using TaxiNT.Libraries.Entities;
using TaxiNT.Libraries.Extensions;
using TaxiNT.Services.Interfaces;

namespace TaxiNT.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftWorkController : ControllerBase
    {
        //Get API Server
        private readonly IShiftWorkService context;
        private readonly ILogger<ShiftWorkController> logger;
        public ShiftWorkController(IShiftWorkService _context, ILogger<ShiftWorkController> _logger)
        {
            this.context = _context;
            this.logger = _logger;
        }

        [HttpPost("upsert-daily")]
        public async Task<IActionResult> UpsertShiftWorkDaily([FromBody] ShiftWorkDailySyncDto data)
        {
            try
            {
                var result = await context.UpsertShiftWorkDailyAsync(data);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error limit get api GoogleSheet");
                return StatusCode(500, "Internal server error");
            }
        }
    }

}

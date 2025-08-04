using Microsoft.AspNetCore.Mvc;
using Backend_SmartBus.Services;
using SmartBus_BusinessObjects.DTOS;

namespace Backend_SmartBus.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class StatisticController : ControllerBase
    {
        private readonly StatisticService _statisticService;

        public StatisticController(StatisticService statisticService)
        {
            _statisticService = statisticService;
        }

        /// <summary>
        /// Thống kê số lượng người dùng theo khoảng thời gian
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> GetUserStatistics([FromBody] StatisticFilterRequest request)
        {
            var result = await _statisticService.GetUserStatisticsAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Thống kê số vé được bán theo khoảng thời gian
        /// </summary>
        [HttpPost("tickets")]
        public async Task<IActionResult> GetTicketStatistics([FromBody] StatisticFilterRequest request)
        {
            var result = await _statisticService.GetTicketStatisticsAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Thống kê doanh thu theo khoảng thời gian
        /// </summary>
        [HttpPost("revenue")]
        public async Task<IActionResult> GetRevenueStatistics([FromBody] StatisticFilterRequest request)
        {
            var result = await _statisticService.GetRevenueStatisticsAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy top tuyến bán chạy nhất
        /// </summary>
        [HttpGet("top-selling-routes")]
        public async Task<IActionResult> GetTopSellingRoutes([FromQuery] int top = 5)
        {
            var result = await _statisticService.GetTopSellingRoutesAsync(top);
            return Ok(result);
        }
    }
}

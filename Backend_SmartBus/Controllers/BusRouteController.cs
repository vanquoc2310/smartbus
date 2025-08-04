using Backend_SmartBus.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend_SmartBus.Controllers
{
    [ApiController]
    [Route("api/busroutes")]
    public class BusRouteController : ControllerBase
    {
        private readonly BusRouteService _routeService;

        public BusRouteController(BusRouteService routeService)
        {
            _routeService = routeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var (routes, total) = await _routeService.GetAllAsync(page, pageSize, search);
            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Data = routes
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(string id)
        {
            var route = await _routeService.GetDetailAsync(id);
            if (route == null) return NotFound();
            return Ok(route);
        }


        [HttpGet("{routeId}/locations")]
        public async Task<IActionResult> GetVehicleLocations(string routeId)
        {
            var result = await _routeService.GetVehicleLocationsAsync(routeId);
            return Ok(result);
        }

    }
}

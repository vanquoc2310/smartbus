using Backend_SmartBus.Services;
using Microsoft.AspNetCore.Mvc;
using SmartBus_BusinessObjects.DTOS;

namespace Backend_SmartBus.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketController : ControllerBase
    {
        private readonly TicketService _service;

        public TicketController(TicketService service)
        {
            _service = service;
        }

        [HttpGet("route/{routeId}/ticket-types")]
        public async Task<IActionResult> GetTicketTypes(string routeId)
        {
            var types = await _service.GetTicketTypesByRouteAsync(routeId);
            return Ok(types);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket(CreateTicketRequest request)
        {
            var ticket = await _service.CreateTicketAsync(request);
            return Ok(ticket);
        }

        [HttpGet("user/{userId}/tickets")]
        public async Task<IActionResult> GetUserTickets(int userId)
        {
            var tickets = await _service.GetUserTicketsAsync(userId);
            return Ok(tickets);
        }
    }
}
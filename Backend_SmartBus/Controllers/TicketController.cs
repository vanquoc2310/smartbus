using Backend_SmartBus.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBus_BusinessObjects.DTOS;

namespace Backend_SmartBus.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketController : ControllerBase
    {
        private readonly TicketService _service;
        private readonly PaymentService _paymentService;
        private readonly SmartBusContext _context;

        public TicketController(TicketService service, PaymentService paymentService, SmartBusContext context)
        {
            _service = service;
            _paymentService = paymentService;
            _context = context;
        }

        [HttpGet("route/{routeId}/ticket-types")]
        public async Task<IActionResult> GetTicketTypes(string routeId)
        {
            var types = await _service.GetTicketTypesByRouteAsync(routeId);
            return Ok(types);
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePaymentForTickets([FromBody] CreateTicketRequest request)
        {
            try
            {
                var paymentResult = await _paymentService.CreatePaymentLinkAsync(request);
                return Ok(new { checkoutUrl = paymentResult.checkoutUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("user/{userId}/tickets")]
        public async Task<IActionResult> GetUserTickets(int userId)
        {
            var tickets = await _service.GetUserTicketsAsync(userId);
            return Ok(tickets);
        }

        [HttpPost("use")]
        public async Task<IActionResult> UseTicket([FromBody] string qrcode)
        {
            if (string.IsNullOrEmpty(qrcode))
            {
                return BadRequest("QR code không hợp lệ.");
            }

            var (isSuccess, message) = await _service.UseTicketAsync(qrcode);

            if (isSuccess)
            {
                return Ok(new { success = true, message = message });
            }
            else
            {
                return BadRequest(new { success = false, message = message });
            }
        }
    }
}
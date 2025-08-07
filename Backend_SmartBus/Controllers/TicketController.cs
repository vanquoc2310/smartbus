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
        public async Task<IActionResult> CreatePaymentForTicket([FromBody] CreateTicketRequest request)
        {
            try
            {
                var priceEntry = await _context.RouteTicketPrices
                    .FirstOrDefaultAsync(r => r.RouteId == request.RouteId && r.TicketTypeId == request.TicketTypeId);

                if (priceEntry == null || priceEntry.Price == null)
                {
                    // Trả về lỗi rõ ràng nếu không tìm thấy giá hoặc giá là null
            return NotFound(new { message = "Không tìm thấy giá vé hoặc giá không hợp lệ cho tuyến đường và loại vé này." });
                }

                // Lấy giá trị decimal từ priceEntry.Price
                decimal price = priceEntry.Price.Value;

                var paymentResult = await _paymentService.CreatePaymentLinkAsync(request, price);

                return Ok(new { checkoutUrl = paymentResult.checkoutUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
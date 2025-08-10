using Backend_SmartBus.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmartBus_BusinessObjects.DTOS;
using System.Text;

namespace Backend_SmartBus.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketController : ControllerBase
    {
        private readonly TicketService _service;
        private readonly PaymentService _paymentService;
        private readonly SmartBusContext _context;
        private readonly IHttpClientFactory _httpClientFactory;


        public TicketController(TicketService service, PaymentService paymentService, SmartBusContext context, IHttpClientFactory httpClientFactory)
        {
            _service = service;
            _paymentService = paymentService;
            _context = context;
            _httpClientFactory = httpClientFactory;
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

        // Endpoint POST hiện tại, nhận DTO
        [HttpPost("use")]
        public async Task<IActionResult> UseTicket([FromBody] QrCodeDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Qrcode))
            {
                return BadRequest("QR code không hợp lệ.");
            }

            var (isSuccess, message) = await _service.UseTicketAsync(request.Qrcode);

            if (isSuccess)
            {
                return Ok(new { success = true, message = message });
            }
            else
            {
                return BadRequest(new { success = false, message = message });
            }
        }

        // Endpoint GET mới để xử lý yêu cầu từ QR code
        [HttpGet("use-by-get/{qrcode}")]
        public async Task<IActionResult> UseTicketByGet(string qrcode)
        {
            if (string.IsNullOrEmpty(qrcode))
            {
                return BadRequest("QR code không hợp lệ.");
            }

            // Tạo một đối tượng DTO từ QR code nhận được
            var postDto = new QrCodeDto { Qrcode = qrcode };

            // Chuyển DTO thành JSON
            var content = new StringContent(JsonConvert.SerializeObject(postDto), Encoding.UTF8, "application/json");

            // Tạo HttpClient và gửi yêu cầu POST nội bộ
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync("https://smartbus-68ae.onrender.com/api/tickets/use", content);

            // Trả về kết quả từ API POST
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return Ok(result);
            }
            else
            {
                var errorResult = await response.Content.ReadAsStringAsync();
                return BadRequest(errorResult);
            }
        }
    }
}
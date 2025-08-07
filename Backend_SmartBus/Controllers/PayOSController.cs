namespace Backend_SmartBus.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Backend_SmartBus.Services;
    using static Backend_SmartBus.Services.PaymentService; // Thêm dòng này để truy cập PaymentSuccessResponse

    [Route("api/[controller]")]
    [ApiController]
    public class PayOSController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PayOSController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("success")]
        public async Task<IActionResult> HandleSuccessReturn([FromQuery] long orderCode)
        {
            try
            {
                var result = await _paymentService.HandleSuccessfulPayment(orderCode);

                if (result != null)
                {
                    // Trả về đối tượng PaymentSuccessResponse
                    return Ok(result);
                }

                return BadRequest(new { message = "Payment processing failed." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
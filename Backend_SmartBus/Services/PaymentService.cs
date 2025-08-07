using Microsoft.Extensions.Options;
using System.Text.Json;
using Backend_SmartBus.Configs;
using Microsoft.EntityFrameworkCore;
using SmartBus_BusinessObjects.DTOS;
using SmartBus_BusinessObjects.Models;
using Net.payOS;
using Net.payOS.Types;
using System.Net;

namespace Backend_SmartBus.Services
{
    public class PaymentService
    {
        private readonly PayOS _payOS;
        private readonly PayOSConfig _payOSConfig;
        private readonly SmartBusContext _context;
        private readonly TicketService _ticketService;

        public PaymentService(IOptions<PayOSConfig> payOSConfig, SmartBusContext context, TicketService ticketService)
        {
            _payOSConfig = payOSConfig.Value;
            _context = context;
            _ticketService = ticketService;

            // Khởi tạo PayOS với các giá trị từ cấu hình
            _payOS = new PayOS(
                _payOSConfig.ClientId,
                _payOSConfig.ApiKey,
                _payOSConfig.ChecksumKey
            );
        }

        public async Task<CreatePaymentResult> CreatePaymentLinkAsync(CreateTicketRequest request, decimal price)
        {
            // Tạo orderCode duy nhất bằng cách kết hợp timestamp và số ngẫu nhiên
            var random = new Random();
            var orderCodeString = DateTimeOffset.Now.ToUnixTimeSeconds().ToString() + random.Next(100, 999).ToString();
            var orderCode = long.Parse(orderCodeString);

            // PayOS yêu cầu giá trị tiền tệ là kiểu số nguyên, vì thế cần ép kiểu
            int amount = (int)price;

            // Sử dụng các biến đã được gán giá trị từ PayOSConfig
            var cancelUrl = _payOSConfig.CancelUrl;
            var returnUrl = _payOSConfig.ReturnUrl;

            // Kiểm tra các URL trước khi sử dụng để tránh lỗi null
            if (string.IsNullOrEmpty(cancelUrl) || string.IsNullOrEmpty(returnUrl))
            {
                throw new InvalidOperationException("CancelUrl or ReturnUrl is not configured.");
            }

            var paymentData = new PaymentData(
                orderCode,
                amount,
                $"Thanh toán SmartBus",
                new List<ItemData>
                {
                    new ItemData("Vé xe buýt", 1, amount)
                },
                cancelUrl,
                returnUrl
            );

            var createPaymentResult = await _payOS.createPaymentLink(paymentData);

            var order = new PaymentOrder
            {
                OrderCode = orderCode,
                Status = "PENDING",
                TicketRequestJson = JsonSerializer.Serialize(request),
                CreatedDate = DateTime.UtcNow
            };
            _context.PaymentOrders.Add(order);
            await _context.SaveChangesAsync();

            return createPaymentResult;
        }

        public class PaymentSuccessResponse
        {
            public string TicketId { get; set; }
            public string TicketType { get; set; }
            public string RouteName { get; set; }
        }

        public async Task<PaymentSuccessResponse> HandleSuccessfulPayment(long orderCode)
        {
            var order = await _context.PaymentOrders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null || order.Status == "PAID") return null;

            var paymentInfo = await _payOS.getPaymentLinkInformation(orderCode);
            if (paymentInfo.status != "PAID") return null;

            order.Status = "PAID";
            _context.Entry(order).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var createTicketRequest = JsonSerializer.Deserialize<CreateTicketRequest>(order.TicketRequestJson);

            var newTicketResponse = await _ticketService.CreateTicketAsync(createTicketRequest);

            return new PaymentSuccessResponse
            {
                TicketId = newTicketResponse.Qrcode,
                TicketType = newTicketResponse.TicketTypeName,
                RouteName = newTicketResponse.RouteName
            };
        }
    }
}
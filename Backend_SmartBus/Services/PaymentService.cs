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

        public async Task<CreatePaymentResult> CreatePaymentLinkAsync(CreateTicketRequest request)
        {
            var orderCodeString = DateTimeOffset.Now.ToUnixTimeSeconds().ToString() + new Random().Next(100, 999).ToString();
            var orderCode = long.Parse(orderCodeString);

            var payosItems = new List<ItemData>();
            decimal totalPrice = 0;

            foreach (var item in request.Items)
            {
                var priceEntry = await _context.RouteTicketPrices
                    .FirstOrDefaultAsync(r => r.RouteId == item.RouteId && r.TicketTypeId == item.TicketTypeId);

                if (priceEntry == null || priceEntry.Price == null)
                    throw new Exception($"Không tìm thấy giá vé cho tuyến {item.RouteId} và loại vé {item.TicketTypeId}");

                payosItems.Add(new ItemData($"Vé xe buýt tuyến {item.RouteId}, loại {item.TicketTypeId}", 1, (int)priceEntry.Price.Value));

                totalPrice += priceEntry.Price.Value;
            }

            int amount = (int)totalPrice;

            var cancelUrl = _payOSConfig.CancelUrl;
            var returnUrl = _payOSConfig.ReturnUrl;

            if (string.IsNullOrEmpty(cancelUrl) || string.IsNullOrEmpty(returnUrl))
            {
                throw new InvalidOperationException("CancelUrl hoặc ReturnUrl chưa được cấu hình.");
            }

            var paymentData = new PaymentData(
                orderCode,
                amount,
                $"Thanh toán SmartBus",
                payosItems,
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

        public async Task<List<PaymentSuccessResponse>> HandleSuccessfulPayment(long orderCode)
        {
            var order = await _context.PaymentOrders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null || order.Status == "PAID") return null;

            var paymentInfo = await _payOS.getPaymentLinkInformation(orderCode);
            if (paymentInfo.status != "PAID") return null;

            order.Status = "PAID";
            _context.Entry(order).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var createTicketRequest = JsonSerializer.Deserialize<CreateTicketRequest>(order.TicketRequestJson);

            var successResponses = new List<PaymentSuccessResponse>();

            foreach (var item in createTicketRequest.Items)
            {
                var newTicketResponse = await _ticketService.CreateTicketAsync(new CreateTicketRequestForSingleTicket
                {
                    UserId = createTicketRequest.UserId,
                    RouteId = item.RouteId,
                    TicketTypeId = item.TicketTypeId
                });

                successResponses.Add(new PaymentSuccessResponse
                {
                    TicketId = newTicketResponse.Qrcode,
                    TicketType = newTicketResponse.TicketTypeName,
                    RouteName = newTicketResponse.RouteName
                });
            }

            return successResponses;
        }

    }
}
using Microsoft.EntityFrameworkCore;
using SmartBus_BusinessObjects.DTOS;
using SmartBus_BusinessObjects.Models;

namespace Backend_SmartBus.Services
{
    public class TicketService
    {
        private readonly SmartBusContext _context;

        public TicketService(SmartBusContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TicketTypeDisplayDTO>> GetTicketTypesByRouteAsync(string routeId)
        {
            return await _context.RouteTicketPrices
                .Where(r => r.RouteId == routeId)
                .Select(r => new TicketTypeDisplayDTO
                {
                    TicketTypeId = r.TicketTypeId.Value,
                    TicketName = r.TicketName,
                    Price = r.Price
                })
                .ToListAsync();
        }

        public async Task<TicketResponse> CreateTicketAsync(CreateTicketRequestForSingleTicket request)
        {
            var now = DateTime.UtcNow;

            Console.WriteLine($"Received TicketTypeId: {request.TicketTypeId}"); // Thêm dòng này để in ra Id
            var ticketType = await _context.TicketTypes.FindAsync(request.TicketTypeId);

            if (ticketType == null)
                throw new Exception("Ticket type not found");

            var priceEntry = await _context.RouteTicketPrices
                .Include(r => r.Route)
                .FirstOrDefaultAsync(r => r.RouteId == request.RouteId && r.TicketTypeId == request.TicketTypeId);

            if (priceEntry == null || priceEntry.Price == null)
                throw new Exception("Ticket price not found or invalid for this route and type");

            var ticket = new Ticket
            {
                UserId = request.UserId,
                RouteId = request.RouteId,
                TicketTypeId = request.TicketTypeId,
                Qrcode = Guid.NewGuid().ToString(),
                IssuedAt = now,
                ExpiredAt = ticketType.DurationDays.HasValue ? now.AddDays(ticketType.DurationDays.Value) : null,
                RemainingUses = ticketType.MaxUses ?? (ticketType.IsUnlimited == true ? null : 1),
                IsActive = true,
                Price = priceEntry.Price.Value
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return new TicketResponse
            {
                Qrcode = ticket.Qrcode,
                Price = ticket.Price,
                IssuedAt = ticket.IssuedAt,
                ExpiredAt = ticket.ExpiredAt,
                RemainingUses = ticket.RemainingUses,
                TicketTypeName = ticketType.Name,
                RouteName = priceEntry.Route.RouteName
            };
        }


        public async Task<IEnumerable<TicketResponse>> GetUserTicketsAsync(int userId)
        {
            return await _context.Tickets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.IssuedAt)
                .Select(t => new TicketResponse
                {
                    Qrcode = t.Qrcode,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    ExpiredAt = t.ExpiredAt,
                    RemainingUses = t.RemainingUses,
                    TicketTypeName = t.TicketType.Name,
                    RouteId = t.RouteId, // Thêm dòng này để trả về RouteId
                    RouteName = t.Route.RouteName
                })
                .ToListAsync();
        }

        public async Task<(bool, string)> UseTicketAsync(int ticketId)
        {
            // 1. Tìm vé và các thông tin liên quan
            var ticket = await _context.Tickets
                .Include(t => t.TicketType)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            // 2. Kiểm tra vé có tồn tại không
            if (ticket == null)
            {
                return (false, "Vé không tồn tại.");
            }

            // 3. Kiểm tra vé có còn hoạt động không
            if (ticket.IsActive != true)
            {
                return (false, "Vé không còn hoạt động.");
            }

            // 4. Kiểm tra vé hết hạn chưa
            if (ticket.ExpiredAt.HasValue && ticket.ExpiredAt.Value < DateTime.Now)
            {
                ticket.IsActive = false;
                await _context.SaveChangesAsync();
                return (false, "Vé đã hết hạn sử dụng.");
            }

            // 5. Kiểm tra logic dựa trên loại vé
            var ticketType = ticket.TicketType;
            if (ticketType != null)
            {
                // Lấy User Id để ghi vào log
                var scannedBy = ticket.UserId.HasValue ? ticket.UserId.Value.ToString() : "N/A";

                if (ticketType.IsUnlimited == true)
                {
                    return await LogTicketUsage(ticket, true, scannedBy, "Vé không giới hạn. Sử dụng thành công.");
                }
                else if (ticketType.DurationDays.HasValue && ticketType.DurationDays.Value > 0)
                {
                    return await LogTicketUsage(ticket, true, scannedBy, "Vé theo ngày. Sử dụng thành công.");
                }
                else if (ticketType.MaxUses.HasValue && ticketType.MaxUses.Value > 0)
                {
                    if (ticket.RemainingUses.HasValue && ticket.RemainingUses.Value > 0)
                    {
                        ticket.RemainingUses--;
                        if (ticket.RemainingUses == 0)
                        {
                            ticket.IsActive = false;
                        }
                        return await LogTicketUsage(ticket, true, scannedBy, $"Vé theo lượt. Sử dụng thành công. Còn {ticket.RemainingUses} lượt.");
                    }
                    else
                    {
                        ticket.IsActive = false;
                        await _context.SaveChangesAsync();
                        return (false, "Vé đã hết lượt sử dụng.");
                    }
                }
            }

            return (false, "Không xác định được loại vé.");
        }

        private async Task<(bool, string)> LogTicketUsage(Ticket ticket, bool isValid, string scannedBy, string message)
        {
            // 6. Ghi lại lịch sử sử dụng
            var usageLog = new TicketUsageLog
            {
                TicketId = ticket.Id,
                ScannedAt = DateTime.Now,
                ScannedBy = scannedBy,
                Location = "N/A", // <-- Đặt giá trị mặc định cho location
                IsValidScan = isValid
            };

            _context.TicketUsageLogs.Add(usageLog);
            await _context.SaveChangesAsync();

            return (isValid, message);
        }

    }
}

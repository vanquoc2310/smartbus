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

        public async Task<(bool, string)> UseTicketAsync(string qrcode)
        {
            // 1. Find the ticket by its QR code and include related information
            var ticket = await _context.Tickets
                .Include(t => t.TicketType)
                .FirstOrDefaultAsync(t => t.Qrcode == qrcode);

            // 2. Check if the ticket exists
            if (ticket == null)
            {
                return (false, "Vé không tồn tại.");
            }

            // 3. Check if the ticket is active
            if (ticket.IsActive != true)
            {
                return (false, "Vé không còn hoạt động.");
            }

            // 4. Check ticket expiration
            if (ticket.ExpiredAt.HasValue && ticket.ExpiredAt.Value < DateTime.Now)
            {
                ticket.IsActive = false;
                await _context.SaveChangesAsync();
                return (false, "Vé đã hết hạn sử dụng.");
            }

            // 5. Logic for per-use tickets (MaxUses)
            var ticketType = ticket.TicketType;
            if (ticketType != null && ticketType.MaxUses.HasValue && ticketType.MaxUses.Value > 0)
            {
                if (ticket.RemainingUses.HasValue && ticket.RemainingUses.Value > 0)
                {
                    // Decrement remaining uses and check if it's the last use
                    ticket.RemainingUses--;
                    if (ticket.RemainingUses == 0)
                    {
                        ticket.IsActive = false; // Deactivate the ticket after the last use
                    }

                    // Get User ID for logging
                    var scannedBy = ticket.UserId.HasValue ? ticket.UserId.Value.ToString() : "N/A";

                    return await LogTicketUsage(ticket, true, scannedBy, $"Vé theo lượt. Sử dụng thành công. Còn {ticket.RemainingUses} lượt.");
                }
                else
                {
                    ticket.IsActive = false;
                    await _context.SaveChangesAsync();
                    return (false, "Vé đã hết lượt sử dụng.");
                }
            }

            // Handle other ticket types if they exist, or return an error
            return (false, "Loại vé không hợp lệ hoặc không xác định.");
        }

        private async Task<(bool, string)> LogTicketUsage(Ticket ticket, bool isValid, string scannedBy, string message)
        {
            // 6. Log the ticket usage
            var usageLog = new TicketUsageLog
            {
                TicketId = ticket.Id,
                ScannedAt = DateTime.Now,
                ScannedBy = scannedBy,
                Location = "N/A",
                IsValidScan = isValid
            };

            _context.TicketUsageLogs.Add(usageLog);
            await _context.SaveChangesAsync();

            return (isValid, message);
        }

        public async Task<PagedResult<AllTicketsResponse>> GetAllTicketsAsync(
    int pageNumber,
    int pageSize,
    string? search)
        {
            var query = _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Route)
                .AsQueryable();

            // Tìm kiếm theo tên tuyến
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Route.RouteName.Contains(search));
            }

            var totalRecords = await query.CountAsync();

            var tickets = await query
                .OrderByDescending(t => t.IssuedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new AllTicketsResponse
                {
                    Qrcode = t.Qrcode,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    ExpiredAt = t.ExpiredAt,
                    RemainingUses = t.RemainingUses,
                    TicketTypeName = t.TicketType.Name,
                    RouteId = t.RouteId,
                    RouteName = t.Route.RouteName,
                    CustomerName = t.User.FullName // thêm tên khách
                })
                .ToListAsync();

            return new PagedResult<AllTicketsResponse>
            {
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = tickets
            };
        }


    }
}

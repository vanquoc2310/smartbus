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

    }
}

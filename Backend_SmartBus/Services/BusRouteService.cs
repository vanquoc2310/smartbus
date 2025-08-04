using Microsoft.EntityFrameworkCore;
using SmartBus_BusinessObjects.DTOS;

namespace Backend_SmartBus.Services
{
    public class BusRouteService
    {
        private readonly SmartBusContext _context;

        public BusRouteService(SmartBusContext context)
        {
            _context = context;
        }

        public async Task<(List<BusRouteDTO> routes, int total)> GetAllAsync(int page, int pageSize, string? search)
        {
            var query = _context.BusRoutes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.RouteCode.Contains(search) || r.RouteName.Contains(search));

            var total = await query.CountAsync();

            var routes = await query
                .OrderBy(r => r.RouteCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new BusRouteDTO
                {
                    Id = r.Id,
                    RouteCode = r.RouteCode,
                    RouteName = r.RouteName,
                    DistanceKm = r.DistanceKm,
                    BusType = r.BusType,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    TripsPerDay = r.TripsPerDay,
                    TripDuration = r.TripDuration,
                    TripInterval = r.TripInterval,
                    PathToDestination = r.PathToDestination,
                    PathToStart = r.PathToStart
                }).ToListAsync();

            return (routes, total);
        }

        public async Task<BusRouteDetailDTO?> GetDetailAsync(string id)
        {
            var route = await _context.BusRoutes
                .Include(r => r.RouteStops).ThenInclude(rs => rs.Stop)
                .Include(r => r.RouteSchedules).ThenInclude(rs => rs.Schedule)
                .Include(r => r.RouteTicketPrices)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return null;

            var tripHours = route.RouteSchedules
                .Where(rs => rs.Schedule != null && rs.Schedule.StartTime != null && rs.Schedule.EndTime != null)
                .Select(rs => rs.Schedule!.StartTime!.Value.ToString("HH:mm") + " - " + rs.Schedule!.EndTime!.Value.ToString("HH:mm"))
                .ToList();

            var detail = new BusRouteDetailDTO
            {
                TicketPrices = route.RouteTicketPrices.Select(tp => $"{tp.TicketName}: {tp.Price:N0} VNĐ").ToList(),

                Id = route.Id,
                RouteCode = route.RouteCode,
                RouteName = route.RouteName,
                DistanceKm = route.DistanceKm,
                BusType = route.BusType,
                StartTime = route.StartTime,
                EndTime = route.EndTime,
                TripsPerDay = route.TripsPerDay,
                TripDuration = route.TripDuration,
                TripInterval = route.TripInterval,
                PathToDestination = route.PathToDestination,
                PathToStart = route.PathToStart,
                StopNames = route.RouteStops.Select(rs => rs.Stop.Name).ToList(),
                TripHours = tripHours
            };

            return detail;
        }


        public async Task<List<BusVehicleLocationDTO>> GetVehicleLocationsAsync(string routeId)
        {
            var locations = await _context.BusVehicleLocations
                .Where(l => l.RouteId == routeId)
                .Select(l => new BusVehicleLocationDTO
                {
                    VehicleId = l.VehicleId,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Speed = l.Speed,
                    LastUpdated = l.LastUpdated
                })
                .ToListAsync();

            return locations;
        }

    }


}

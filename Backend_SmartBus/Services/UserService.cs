namespace Backend_SmartBus.Services
{
    using Microsoft.EntityFrameworkCore;
    using SmartBus_BusinessObjects.DTOS;
    using SmartBus_BusinessObjects.Models;

    public class UserService
    {
        private readonly SmartBusContext _context;

        public UserService(SmartBusContext context)
        {
            _context = context;
        }

        public async Task<(List<UserDTO> users, int totalCount)> GetAllAsync(int page, int pageSize, string? search)
        {
            var query = _context.Users.Where(u => u.RoleId == 2); // chỉ lấy RoleId = 2

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search));

            var total = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
                }).ToListAsync();

            return (users, total);
        }

        public async Task<UserDTO?> GetByIdAsync(int id, int? month = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;

            int filterMonth = month ?? DateTime.Now.Month;
            int filterYear = DateTime.Now.Year;

            // Lấy vé của user trong tháng, kèm thông tin tuyến
            var tickets = await _context.Tickets
                .Include(t => t.Route) // join sang BusRoute để lấy DistanceKm
                .Where(t => t.UserId == id &&
                            t.IssuedAt.HasValue &&
                            t.IssuedAt.Value.Month == filterMonth &&
                            t.IssuedAt.Value.Year == filterYear)
                .ToListAsync();

            // Km mỗi ngày (theo ngày phát hành vé)
            var kmPerDay = tickets
                .GroupBy(t => t.IssuedAt.Value.Day)
                .Select(g => new DayKmDTO
                {
                    Day = g.Key,
                    DistanceKm = g.Sum(x => x.Route.DistanceKm ?? 0)
                })
                .OrderBy(x => x.Day)
                .ToList();

            decimal totalKm = kmPerDay.Sum(x => x.DistanceKm);

            // Số chuyến = số vé
            int totalTrips = tickets.Count;

            // Chuyến dài nhất
            var longestTrip = tickets
                .OrderByDescending(t => t.Route.DistanceKm)
                .Select(t => new TripInfoDTO
                {
                    DistanceKm = t.Route.DistanceKm ?? 0,
                    RouteName = t.Route.RouteName
                })
                .FirstOrDefault();

            // Giảm CO₂: ví dụ 1 km = 0.055 kg CO₂
            decimal co2Saved = Math.Round(totalKm * 0.055m, 2);

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                ImageUrl = user.ImageUrl,

                // Thống kê thêm
                TotalKm = totalKm,
                KmPerDay = kmPerDay,
                TotalTrips = totalTrips,
                LongestTrip = longestTrip,
                Co2SavedKg = co2Saved
            };
        }



        public async Task<UserDTO> CreateAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                ImageUrl = user.ImageUrl
            };
        }

        public async Task<UserDTO?> UpdateAsync(int id, UserUpdateDTO updatedUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            user.FullName = updatedUser.FullName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Email = updatedUser.Email;
            user.ImageUrl = updatedUser.ImageUrl;

            await _context.SaveChangesAsync();

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                ImageUrl = user.ImageUrl
            };
        }

        public async Task<(bool success, string message)> DeleteAsync(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return (false, "User not found");

            // Xóa mềm (bất kể đã mua vé hay chưa)
            user.IsActive = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (true, "User đã được vô hiệu hóa (xóa mềm) thành công");
        }

    }
}

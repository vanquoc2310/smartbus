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

        public async Task<UserDTO?> GetByIdAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;

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
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserDTO?> UpdateAsync(int id, User updatedUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            user.FullName = updatedUser.FullName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Email = updatedUser.Email;

            await _context.SaveChangesAsync();

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<(bool success, string message)> DeleteAsync(int id)
        {
            var user = await _context.Users.Include(u => u.Tickets).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return (false, "User not found");
            if (user.Tickets.Any()) return (false, "Không thể xóa người dùng vì đã từng mua vé.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return (true, "User deleted successfully");
        }
    }
}

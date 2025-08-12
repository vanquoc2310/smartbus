namespace Backend_SmartBus.Services
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Tokens;
    using SmartBus_BusinessObjects.DTOS;
    using SmartBus_BusinessObjects.Models;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    public class AuthService
    {
        private readonly SmartBusContext _context;
        private readonly IConfiguration _config;
        private readonly OtpService _otpService;

        public AuthService(SmartBusContext context, IConfiguration config, OtpService otpService)
        {
            _context = context;
            _config = config;
            _otpService = otpService;
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim("id", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.FullName),
            new Claim("role", user.Role?.RoleName ?? "User"),
            new Claim("image", user.ImageUrl ?? string.Empty) // Thêm claim ImageUrl vào đây
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User> Authenticate(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null || user.Password != password)
                return null;

            return user;
        }


        public async Task<bool> SendOtpToEmail(string email)
        {
            return await _otpService.SendOtpAsync(email);
        }

        public async Task<bool> Register(RegisterRequest req)
        {
            if (!_otpService.VerifyOtp(req.Email, req.Otp)) return false;

            if (_context.Users.Any(u => u.Email == req.Email || u.PhoneNumber == req.PhoneNumber))
                return false;

            var newUser = new User
            {
                Email = req.Email,
                Password = req.Password,
                FullName = req.FullName,
                PhoneNumber = req.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                RoleId = _context.Roles.FirstOrDefault(r => r.RoleName == "User")?.Id,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordRequest req)
        {
            if (!_otpService.VerifyOtp(req.Email, req.Otp)) return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user == null) return false;

            user.Password = req.NewPassword; // Should hash in real-world apps
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

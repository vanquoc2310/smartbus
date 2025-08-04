using Backend_SmartBus.Services;
using Microsoft.AspNetCore.Mvc;
using SmartBus_BusinessObjects.DTOS;

namespace Backend_SmartBus.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _authService.Authenticate(req.Email, req.Password);
            if (user == null) return Unauthorized("Invalid credentials");

            var token = _authService.GenerateJwtToken(user);
            return Ok(new { token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var success = await _authService.Register(req);
            if (!success) return BadRequest("Registration failed");
            return Ok("Registered successfully");
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromQuery] string email)
        {
            var sent = await _authService.SendOtpToEmail(email);
            if (!sent) return BadRequest("Failed to send OTP");
            return Ok("OTP sent to email");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var result = await _authService.ResetPassword(req);
            if (!result) return BadRequest("Reset password failed");
            return Ok("Password has been reset successfully");
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok("Logged out (client should delete JWT)");
        }
    }
}

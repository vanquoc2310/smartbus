using Backend_SmartBus.Services;
using Microsoft.AspNetCore.Mvc;
using SmartBus_BusinessObjects.DTOS;
using SmartBus_BusinessObjects.Models;

namespace Backend_SmartBus.Controllers
{

    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var (users, total) = await _userService.GetAllAsync(page, pageSize, search);
            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Data = users
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int? month = null)
        {
            var user = await _userService.GetByIdAsync(id, month);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            var result = await _userService.CreateAsync(user);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDTO updatedUser)
        {
            var user = await _userService.UpdateAsync(id, updatedUser);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _userService.DeleteAsync(id);
            if (!success) return BadRequest(message);
            return Ok(message);
        }
    }
}

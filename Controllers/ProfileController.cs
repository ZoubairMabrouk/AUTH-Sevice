using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AUTH_Sevice.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]   // User OU Admin
    public class ProfileController : ControllerBase
    {
        private readonly IUserRepository _repo;

        public ProfileController(IUserRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _repo.GetByIdAsync(userId, ct);
            if (user is null) return NotFound();

            return Ok(new UserListDto(user.Id, user.FirstName, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt));
        }
    }
}

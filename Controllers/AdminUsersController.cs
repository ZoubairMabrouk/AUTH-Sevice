using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Domain.Entities.Enums;
using AUTH_Sevice.Infrastructure.Repositories;
using AUTH_Sevice.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

namespace AUTH_Sevice.Controllers
{

    [ApiController]
    [Route("api/users")]  // ← Toutes les routes Admin uniquement
    public class AdminUsersController : ControllerBase
    {
        private readonly AdminUserService _service;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(AdminUserService service, ILogger<AdminUsersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserListDto>), 200)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var users = await _service.GetAllUsersAsync(ct);
            return Ok(users);
        }

        /// <summary>GET /api/users/{id}</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserListDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            try
            {
                var user = await _service.GetUserByIdAsync(id, ct);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>POST /api/users — Créer un utilisateur</summary>
        [HttpPost]
        [ProducesResponseType(typeof(UserListDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
        {
            try
            {
                var created = await _service.CreateUserAsync(dto, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>PUT /api/users/{id} — Modifier un utilisateur</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(UserListDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            try
            {
                var updated = await _service.UpdateUserAsync(id, dto, ct);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteUserAsync(id, ct);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(UserListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusDto dto, CancellationToken ct)
        {
            try
            {
                var updated = await _service.UpdateStatusAsync(id, dto, ct);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }

}

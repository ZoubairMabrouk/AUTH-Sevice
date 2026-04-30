using AUTH_Sevice.Application.Common.Intefaces;
using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AUTH_Sevice.Controllers
{

    [ApiController]
    [Route("api/users")]
    [Authorize]
    [Produces("application/json")]
    public class UsersController(ISender mediator, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Get the currently authenticated user's profile.</summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = currentUser.UserId
                         ?? throw new UnauthorizedAccessException("User ID not found in token.");

            var result = await mediator.Send(new GetCurrentUserQuery(userId), ct);
            return Ok(result);
        }

        /// <summary>Get all users. Requires Admin role.</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await mediator.Send(new GetAllUsersQuery(), ct);
            return Ok(result);
        }
    }
}

using AUTH_Sevice.Application.Auth.Commands;
using AUTH_Sevice.Application.Common.Intefaces;
using AUTH_Sevice.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AUTH_Sevice.Controllers
{

    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController(ISender mediator, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Register a new user account.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var command = new RegisterCommand(request.Email, request.Password, request.FirstName, request.LastName);
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetMe), null, result);
        }

        /// <summary>Authenticate and receive JWT tokens.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var ipAddress = currentUser.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var command = new LoginCommand(request.Email, request.Password, ipAddress);
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        /// <summary>Exchange a refresh token for new access + refresh tokens.</summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
            var ipAddress = currentUser.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var command = new RefreshTokenCommand(request.RefreshToken, ipAddress);
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        // Needed for CreatedAtAction reference
        [HttpGet("me-placeholder")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetMe() => Ok();
    }
}

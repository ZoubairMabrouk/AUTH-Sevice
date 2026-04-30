namespace AUTH_Sevice.Application.DTOs
{
    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName);

    public record LoginRequest(
        string Email,
        string Password);

    public record RefreshTokenRequest(
        string RefreshToken);

    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        string TokenType = "Bearer");

    public record UserDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        bool IsActive,
        DateTime CreatedAt);

    public record UserProfileDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        DateTime CreatedAt);

    public record ErrorResponse(
        string Code,
        string Message,
        IEnumerable<string>? Details = null);

    public record ValidationErrorResponse(
        string Code,
        string Message,
        IDictionary<string, string[]> Errors);

}

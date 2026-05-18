namespace AUTH_Sevice.Application.DTOs
{
    public record UserListDto(
        Guid Id,
        string Name,
        string Email,
        string Role,
        bool isActive,
        DateTime CreatedAt
    );
}

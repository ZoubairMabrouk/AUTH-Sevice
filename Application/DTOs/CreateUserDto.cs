namespace AUTH_Sevice.Application.DTOs
{
    public record CreateUserDto(
        string Name,
        string Email,
        string Password,
        string Role = "User"    // "Admin" | "User"
    );
    public record UpdateUserDto(
    string Name,
    string Email,
    string Role,
    bool isActive
);
    public record UpdateUserStatusDto(
    bool isActive// "Active" | "Inactive" | "Blocked"
);
}

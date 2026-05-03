using System.ComponentModel.DataAnnotations;

namespace AuctionSystem.API.DTOs.User;

public class CreateUserDto
{
    [Required][StringLength(50, MinimumLength = 3)]
    public string Username  { get; set; } = string.Empty;
    [Required][EmailAddress][StringLength(200)]
    public string Email     { get; set; } = string.Empty;
    [Required][StringLength(100, MinimumLength = 6)]
    public string Password  { get; set; } = string.Empty;
    [StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [StringLength(100)] public string LastName  { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    [EmailAddress][StringLength(200)]         public string? Email     { get; set; }
    [StringLength(100, MinimumLength = 6)]    public string? Password  { get; set; }
    [StringLength(100)]                       public string? FirstName { get; set; }
    [StringLength(100)]                       public string? LastName  { get; set; }
}

public class LoginDto
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public int       Id        { get; set; }
    public string    Username  { get; set; } = string.Empty;
    public string    Email     { get; set; } = string.Empty;
    public string    Password  { get; set; } = string.Empty;
    public string    FirstName { get; set; } = string.Empty;
    public string    LastName  { get; set; } = string.Empty;
    public DateTime  CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class LoginResponseDto
{
    public string   Token     { get; set; } = string.Empty;
    public int      UserId    { get; set; }
    public string   Username  { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

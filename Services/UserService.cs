using AuctionSystem.API.DTOs.User;
using AuctionSystem.API.Exceptions;
using AuctionSystem.API.Models;
using AuctionSystem.API.Repositories;

namespace AuctionSystem.API.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto>              GetByIdAsync(int id);
    Task<UserDto>              CreateAsync(CreateUserDto dto);
    Task<UserDto>              UpdateAsync(int id, UpdateUserDto dto);
    Task                       DeleteAsync(int id);
    Task<LoginResponseDto>     LoginAsync(LoginDto dto);
}

public class UserService : IUserService
{
    private readonly IUserRepository  _repo;
    private readonly ITokenService    _tokenService;
    private readonly IConfiguration   _config;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repo, ITokenService tokenService,
                       IConfiguration config, ILogger<UserService> logger)
    {
        _repo         = repo;
        _tokenService = tokenService;
        _config       = config;
        _logger       = logger;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
        => (await _repo.GetAllAsync()).Select(ToDto);

    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("User", id);
        return ToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        if (await _repo.UsernameExistsAsync(dto.Username))
            throw new ConflictException($"Username '{dto.Username}' is already taken.");
        if (await _repo.EmailExistsAsync(dto.Email))
            throw new ConflictException($"Email '{dto.Email}' is already registered.");

        var user = new User
        {
            Username  = dto.Username, Email     = dto.Email,
            Password  = dto.Password, FirstName = dto.FirstName,
            LastName  = dto.LastName, CreatedAt = DateTime.UtcNow
        };
        var created = await _repo.CreateAsync(user);
        _logger.LogInformation("User created: id={Id} username={Username}", created.Id, created.Username);
        return ToDto(created);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _repo.GetByUsernameAsync(dto.Username)
            ?? throw new NotFoundException($"User '{dto.Username}' not found.");

        if (user.Password != dto.Password)
            throw new AuctionSystem.API.Exceptions.ValidationException("Incorrect password.");

        var token   = _tokenService.GenerateToken(user);
        var expires = int.Parse(_config["Jwt:ExpiresHours"] ?? "24");
        _logger.LogInformation("User logged in: id={Id} username={Username}", user.Id, user.Username);

        return new LoginResponseDto
        {
            Token     = token,
            UserId    = user.Id,
            Username  = user.Username,
            ExpiresAt = DateTime.UtcNow.AddHours(expires)
        };
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("User", id);
        if (dto.Email is not null)
        {
            if (await _repo.EmailExistsAsync(dto.Email, id))
                throw new ConflictException($"Email '{dto.Email}' is already in use.");
            user.Email = dto.Email;
        }
        if (dto.Password  is not null) user.Password  = dto.Password;
        if (dto.FirstName is not null) user.FirstName = dto.FirstName;
        if (dto.LastName  is not null) user.LastName  = dto.LastName;
        user.UpdatedAt = DateTime.UtcNow;
        var updated = await _repo.UpdateAsync(user);
        _logger.LogInformation("User updated: id={Id}", updated.Id);
        return ToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("User", id);
        await _repo.DeleteAsync(user);
        _logger.LogInformation("User deleted: id={Id}", id);
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id, Username = u.Username, Email = u.Email, Password = u.Password,
        FirstName = u.FirstName, LastName = u.LastName,
        CreatedAt = u.CreatedAt, UpdatedAt = u.UpdatedAt
    };
}

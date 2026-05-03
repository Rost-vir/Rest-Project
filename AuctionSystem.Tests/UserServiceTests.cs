using AuctionSystem.API.DTOs.User;
using AuctionSystem.API.Exceptions;
using AuctionSystem.API.Models;
using AuctionSystem.API.Repositories;
using AuctionSystem.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AuctionSystem.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repo         = new();
    private readonly Mock<ITokenService>   _tokenService = new();
    private readonly UserService           _sut;

    public UserServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]       = "test-secret-key-minimum-32-characters!!",
                ["Jwt:ExpiresHours"] = "24"
            })
            .Build();

        _sut = new UserService(_repo.Object, _tokenService.Object,
            config, NullLogger<UserService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedUser()
    {
        var dto = new CreateUserDto
        {
            Username  = "newuser",
            Email     = "new@example.com",
            Password  = "password123",
            FirstName = "Jan",
            LastName  = "Kowalski"
        };
        _repo.Setup(r => r.UsernameExistsAsync("newuser")).ReturnsAsync(false);
        _repo.Setup(r => r.EmailExistsAsync("new@example.com")).ReturnsAsync(false);
        _repo.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = 1; return u; });

        var result = await _sut.CreateAsync(dto);

        result.Id.Should().Be(1);
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task CreateAsync_DuplicateUsername_ThrowsConflictException()
    {
        var dto = new CreateUserDto { Username = "existing", Email = "e@e.com", Password = "pass123" };
        _repo.Setup(r => r.UsernameExistsAsync("existing")).ReturnsAsync(true);

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*existing*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsConflictException()
    {
        var dto = new CreateUserDto { Username = "newuser", Email = "taken@example.com", Password = "pass123" };
        _repo.Setup(r => r.UsernameExistsAsync("newuser")).ReturnsAsync(false);
        _repo.Setup(r => r.EmailExistsAsync("taken@example.com")).ReturnsAsync(true);

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*taken@example.com*");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var user = MakeUser();
        _repo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateToken(user)).Returns("jwt.token.here");

        var result = await _sut.LoginAsync(new LoginDto { Username = "testuser", Password = "password123" });

        result.Token.Should().Be("jwt.token.here");
        result.UserId.Should().Be(1);
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsValidationException()
    {
        var user = MakeUser();
        _repo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var act = () => _sut.LoginAsync(new LoginDto { Username = "testuser", Password = "wrongpass" });

        await act.Should().ThrowAsync<AuctionSystem.API.Exceptions.ValidationException>()
            .WithMessage("*password*");
    }

    [Fact]
    public async Task LoginAsync_NonExistingUser_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.GetByUsernameAsync("ghost")).ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginDto { Username = "ghost", Password = "pass" });

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*ghost*");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsUserDto()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());

        var result = await _sut.GetByIdAsync(1);

        result.Id.Should().Be(1);
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesUser()
    {
        var user = MakeUser();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _repo.Setup(r => r.EmailExistsAsync("newemail@example.com", 1)).ReturnsAsync(false);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _sut.UpdateAsync(1, new UpdateUserDto { Email = "newemail@example.com", FirstName = "Marek" });

        result.Email.Should().Be("newemail@example.com");
        result.FirstName.Should().Be("Marek");
    }

    [Fact]
    public async Task UpdateAsync_EmailAlreadyInUse_ThrowsConflictException()
    {
        var user = MakeUser();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _repo.Setup(r => r.EmailExistsAsync("taken@example.com", 1)).ReturnsAsync(true);

        var act = () => _sut.UpdateAsync(1, new UpdateUserDto { Email = "taken@example.com" });

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*taken@example.com*");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingUser_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var act = () => _sut.UpdateAsync(99, new UpdateUserDto { FirstName = "X" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_CallsDelete()
    {
        var user = MakeUser();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _repo.Setup(r => r.DeleteAsync(user)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(1);

        _repo.Verify(r => r.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingUser_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var act = () => _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
        {
            MakeUser(1, "user1"),
            MakeUser(2, "user2")
        });

        var result = (await _sut.GetAllAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Username.Should().Be("user1");
        result[1].Username.Should().Be("user2");
    }

    private static User MakeUser(int id = 1, string username = "testuser") => new()
    {
        Id        = id,
        Username  = username,
        Email     = $"{username}@example.com",
        Password  = "password123",
        FirstName = "Test",
        LastName  = "User",
        CreatedAt = DateTime.UtcNow
    };
}

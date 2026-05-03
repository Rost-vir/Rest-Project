using AuctionSystem.API.Data;
using AuctionSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.API.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?>             GetByIdAsync(int id);
    Task<User?>             GetByUsernameAsync(string username);
    Task<bool>              ExistsAsync(int id);
    Task<bool>              UsernameExistsAsync(string username, int? excludeId = null);
    Task<bool>              EmailExistsAsync(string email, int? excludeId = null);
    Task<User>              CreateAsync(User user);
    Task<User>              UpdateAsync(User user);
    Task                    DeleteAsync(User user);
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    public UserRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<User>> GetAllAsync()
        => await _context.Users.AsNoTracking().ToListAsync();

    public async Task<User?> GetByIdAsync(int id)
        => await _context.Users.FindAsync(id);

    public async Task<User?> GetByUsernameAsync(string username)
        => await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<bool> ExistsAsync(int id)
        => await _context.Users.AnyAsync(u => u.Id == id);

    public async Task<bool> UsernameExistsAsync(string username, int? excludeId = null)
        => await _context.Users.AnyAsync(u =>
            u.Username == username && (excludeId == null || u.Id != excludeId));

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        => await _context.Users.AnyAsync(u =>
            u.Email == email && (excludeId == null || u.Id != excludeId));

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}

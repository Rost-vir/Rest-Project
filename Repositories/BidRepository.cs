using AuctionSystem.API.Data;
using AuctionSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.API.Repositories;

public interface IBidRepository
{
    Task<IEnumerable<Bid>> GetByAuctionIdAsync(int auctionId);
    Task<Bid?> GetByIdAsync(int id);
    Task<Bid> CreateAsync(Bid bid);
}

public class BidRepository : IBidRepository
{
    private readonly AppDbContext _context;
    public BidRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Bid>> GetByAuctionIdAsync(int auctionId)
        => await _context.Bids.Include(b => b.Bidder)
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.PlacedAt).AsNoTracking().ToListAsync();

    public async Task<Bid?> GetByIdAsync(int id)
        => await _context.Bids.Include(b => b.Bidder).FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Bid> CreateAsync(Bid bid)
    {
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        return await _context.Bids.Include(b => b.Bidder).FirstAsync(b => b.Id == bid.Id);
    }
}

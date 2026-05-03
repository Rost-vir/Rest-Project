using AuctionSystem.API.Data;
using AuctionSystem.API.DTOs.Auction;
using AuctionSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.API.Repositories;

public interface IAuctionRepository
{
    Task<(IEnumerable<Auction> Items, int TotalCount)> GetAllAsync(AuctionQueryDto query);
    Task<Auction?> GetByIdAsync(int id);
    Task<Auction?> GetByIdWithBidsAsync(int id);
    Task<Auction> AddAsync(Auction auction);
    Task<Auction> UpdateAsync(Auction auction);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

public class AuctionRepository : IAuctionRepository
{
    private readonly AppDbContext _context;
    public AuctionRepository(AppDbContext context) => _context = context;

    public async Task<(IEnumerable<Auction> Items, int TotalCount)> GetAllAsync(AuctionQueryDto query)
    {
        var q = _context.Auctions.Include(a => a.Owner).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Category))
            q = q.Where(a => a.Category == query.Category);
        if (query.Status.HasValue)
            q = q.Where(a => a.Status == query.Status.Value);

        var totalCount = await q.CountAsync();

        var desc = query.Order?.ToLower() == "desc";
        q = query.SortBy?.ToLower() switch
        {
            "startingprice" => desc ? q.OrderByDescending(a => (double)a.StartingPrice) : q.OrderBy(a => (double)a.StartingPrice),
            "currentprice" => desc ? q.OrderByDescending(a => (double)a.CurrentPrice) : q.OrderBy(a => (double)a.CurrentPrice),
            "enddate"       => desc ? q.OrderByDescending(a => a.EndDate)        : q.OrderBy(a => a.EndDate),
            "startdate"     => desc ? q.OrderByDescending(a => a.StartDate)      : q.OrderBy(a => a.StartDate),
            "name"          => desc ? q.OrderByDescending(a => a.Name)           : q.OrderBy(a => a.Name),
            _               => q.OrderBy(a => a.Id)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<Auction?> GetByIdAsync(int id)
        => await _context.Auctions.Include(a => a.Owner).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Auction?> GetByIdWithBidsAsync(int id)
        => await _context.Auctions
            .Include(a => a.Owner)
            .Include(a => a.Bids).ThenInclude(b => b.Bidder)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Auction> AddAsync(Auction auction)
    {
        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync();
        return await _context.Auctions.Include(a => a.Owner).FirstAsync(a => a.Id == auction.Id);
    }

    public async Task<Auction> UpdateAsync(Auction auction)
    {
        _context.Auctions.Update(auction);
        await _context.SaveChangesAsync();
        return await _context.Auctions.Include(a => a.Owner).FirstAsync(a => a.Id == auction.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var a = await _context.Auctions.FindAsync(id);
        if (a is not null) { _context.Auctions.Remove(a); await _context.SaveChangesAsync(); }
    }

    public async Task<bool> ExistsAsync(int id) => await _context.Auctions.AnyAsync(a => a.Id == id);
}

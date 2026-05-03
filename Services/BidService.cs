using AuctionSystem.API.DTOs.Auction;
using AuctionSystem.API.Exceptions;
using AuctionSystem.API.Models;
using AuctionSystem.API.Repositories;

namespace AuctionSystem.API.Services;

public interface IBidService
{
    Task<IEnumerable<BidDto>> GetByAuctionAsync(int auctionId);
    Task<BidDto> PlaceBidAsync(int auctionId, PlaceBidDto dto);
}

public class BidService : IBidService
{
    private readonly IBidRepository      _bidRepo;
    private readonly IAuctionRepository  _auctionRepo;
    private readonly IUserRepository     _userRepo;
    private readonly ILogger<BidService> _logger;

    public BidService(IBidRepository bidRepo, IAuctionRepository auctionRepo, IUserRepository userRepo, ILogger<BidService> logger)
    {
        _bidRepo     = bidRepo;
        _auctionRepo = auctionRepo;
        _userRepo    = userRepo;
        _logger      = logger;
    }

    public async Task<IEnumerable<BidDto>> GetByAuctionAsync(int auctionId)
    {
        if (!await _auctionRepo.ExistsAsync(auctionId))
            throw new NotFoundException("Auction", auctionId);
        _logger.LogInformation("GetBids auctionId={AuctionId}", auctionId);
        var bids = await _bidRepo.GetByAuctionIdAsync(auctionId);
        return bids.Select(ToDto);
    }

    public async Task<BidDto> PlaceBidAsync(int auctionId, PlaceBidDto dto)
    {
        var auction = await _auctionRepo.GetByIdAsync(auctionId)
            ?? throw new NotFoundException("Auction", auctionId);

        if (auction.Status == AuctionStatus.Cancelled)
            throw new BusinessRuleException("Cannot bid on a cancelled auction.");

        if (auction.Status == AuctionStatus.Ended || auction.EndDate <= DateTime.UtcNow)
        {
            if (auction.Status != AuctionStatus.Ended)
            {
                auction.Status = AuctionStatus.Ended;
                await _auctionRepo.UpdateAsync(auction);
                _logger.LogInformation("Auction auto-closed id={AuctionId}", auctionId);
            }
            throw new BusinessRuleException("Auction has already ended. Bidding is closed.");
        }

        if (auction.OwnerId == dto.BidderId)
            throw new BusinessRuleException("Auction owner cannot bid on their own auction.");

        if (!await _userRepo.ExistsAsync(dto.BidderId))
            throw new NotFoundException("User", dto.BidderId);

        if (dto.Amount <= auction.CurrentPrice)
            throw new BusinessRuleException(
                $"Bid amount {dto.Amount:F2} must be higher than current price {auction.CurrentPrice:F2}.");

        var bid = new Bid { AuctionId = auctionId, BidderId = dto.BidderId, Amount = dto.Amount, PlacedAt = DateTime.UtcNow };
        var created = await _bidRepo.CreateAsync(bid);

        auction.CurrentPrice = dto.Amount;
        await _auctionRepo.UpdateAsync(auction);

        _logger.LogInformation("Bid placed auctionId={AuctionId} bidderId={BidderId} amount={Amount}",
            auctionId, dto.BidderId, dto.Amount);
        return ToDto(created);
    }

    private static BidDto ToDto(Bid b) => new()
    {
        Id = b.Id, Amount = b.Amount, PlacedAt = b.PlacedAt,
        AuctionId = b.AuctionId, BidderId = b.BidderId,
        BidderUsername = b.Bidder?.Username ?? string.Empty
    };
}

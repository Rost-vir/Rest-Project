using AuctionSystem.API.DTOs.Auction;
using AuctionSystem.API.Exceptions;
using AuctionSystem.API.Models;
using AuctionSystem.API.Repositories;

namespace AuctionSystem.API.Services;

public interface IAuctionService
{
    Task<PagedResult<AuctionDto>> GetAllAsync(AuctionQueryDto query);
    Task<AuctionDto> GetByIdAsync(int id);
    Task<AuctionDto> CreateAsync(CreateAuctionDto dto);
    Task<AuctionDto> UpdateAsync(int id, UpdateAuctionDto dto);
    Task DeleteAsync(int id);
}

public class AuctionService : IAuctionService
{
    private readonly IAuctionRepository  _auctionRepo;
    private readonly IUserRepository     _userRepo;
    private readonly ILogger<AuctionService> _logger;

    public AuctionService(IAuctionRepository auctionRepo, IUserRepository userRepo, ILogger<AuctionService> logger)
    {
        _auctionRepo = auctionRepo;
        _userRepo    = userRepo;
        _logger      = logger;
    }

    public async Task<PagedResult<AuctionDto>> GetAllAsync(AuctionQueryDto query)
    {
        _logger.LogInformation("GetAllAuctions page={Page} pageSize={PageSize} category={Category} status={Status} sortBy={SortBy} order={Order}",
            query.Page, query.PageSize, query.Category, query.Status, query.SortBy, query.Order);
        var (items, total) = await _auctionRepo.GetAllAsync(query);
        return new PagedResult<AuctionDto>
        {
            Data = items.Select(ToDto), TotalCount = total, Page = query.Page, PageSize = query.PageSize
        };
    }

    public async Task<AuctionDto> GetByIdAsync(int id)
    {
        var auction = await _auctionRepo.GetByIdWithBidsAsync(id)
            ?? throw new NotFoundException("Auction", id);
        _logger.LogInformation("GetAuction id={Id}", id);
        return ToDto(auction);
    }

    public async Task<AuctionDto> CreateAsync(CreateAuctionDto dto)
    {
        if (dto.EndDate <= dto.StartDate)
            throw new ValidationException("EndDate must be after StartDate.");
        if (!await _userRepo.ExistsAsync(dto.OwnerId))
            throw new NotFoundException("User", dto.OwnerId);

        var auction = new Auction
        {
            Name = dto.Name, Description = dto.Description, Category = dto.Category,
            StartingPrice = dto.StartingPrice, CurrentPrice = dto.StartingPrice,
            StartDate = dto.StartDate, EndDate = dto.EndDate,
            OwnerId = dto.OwnerId, Status = AuctionStatus.Active
        };
        var created = await _auctionRepo.AddAsync(auction);
        _logger.LogInformation("Auction created id={Id} name={Name} ownerId={OwnerId}", created.Id, created.Name, created.OwnerId);
        return ToDto(created);
    }

    public async Task<AuctionDto> UpdateAsync(int id, UpdateAuctionDto dto)
    {
        var auction = await _auctionRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("Auction", id);
        if (auction.Status == AuctionStatus.Ended || auction.Status == AuctionStatus.Cancelled)
            throw new BusinessRuleException("Cannot update an auction that has ended or been cancelled.");
        if (dto.EndDate.HasValue && dto.EndDate.Value <= DateTime.UtcNow)
            throw new ValidationException("EndDate must be in the future.");

        if (dto.Name        is not null) auction.Name        = dto.Name;
        if (dto.Description is not null) auction.Description = dto.Description;
        if (dto.Category    is not null) auction.Category    = dto.Category;
        if (dto.EndDate.HasValue)        auction.EndDate     = dto.EndDate.Value;
        if (dto.Status.HasValue)         auction.Status      = dto.Status.Value;

        var updated = await _auctionRepo.UpdateAsync(auction);
        _logger.LogInformation("Auction updated id={Id}", id);
        return ToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        if (!await _auctionRepo.ExistsAsync(id))
            throw new NotFoundException("Auction", id);
        await _auctionRepo.DeleteAsync(id);
        _logger.LogInformation("Auction deleted id={Id}", id);
    }

    private static AuctionDto ToDto(Auction a) => new()
    {
        Id = a.Id, Name = a.Name, Description = a.Description, Category = a.Category,
        StartingPrice = a.StartingPrice, CurrentPrice = a.CurrentPrice,
        StartDate = a.StartDate, EndDate = a.EndDate, Status = a.Status.ToString(),
        OwnerId = a.OwnerId, OwnerUsername = a.Owner?.Username ?? string.Empty,
        BidCount = a.Bids?.Count ?? 0
    };
}

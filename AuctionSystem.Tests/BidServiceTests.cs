using AuctionSystem.API.DTOs.Auction;
using AuctionSystem.API.Exceptions;
using AuctionSystem.API.Models;
using AuctionSystem.API.Repositories;
using AuctionSystem.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AuctionSystem.Tests;

public class BidServiceTests
{
    private readonly Mock<IBidRepository>     _bidRepo     = new();
    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IUserRepository>    _userRepo    = new();
    private readonly BidService               _sut;

    public BidServiceTests()
    {
        _sut = new BidService(_bidRepo.Object, _auctionRepo.Object,
            _userRepo.Object, NullLogger<BidService>.Instance);
    }

    [Fact]
    public async Task PlaceBidAsync_ValidBid_ReturnsCreatedBid()
    {
        var auction = MakeAuction(currentPrice: 100m);
        var dto     = new PlaceBidDto { Amount = 150m, BidderId = 2 };

        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);
        _userRepo.Setup(r => r.ExistsAsync(2)).ReturnsAsync(true);
        _bidRepo.Setup(r => r.CreateAsync(It.IsAny<Bid>()))
            .ReturnsAsync((Bid b) => { b.Id = 10; b.Bidder = new User { Username = "bidder" }; return b; });
        _auctionRepo.Setup(r => r.UpdateAsync(It.IsAny<Auction>())).ReturnsAsync((Auction a) => a);

        var result = await _sut.PlaceBidAsync(1, dto);

        result.Amount.Should().Be(150m);
        result.Id.Should().Be(10);
    }

    [Fact]
    public async Task PlaceBidAsync_AmountNotHigherThanCurrentPrice_ThrowsBusinessRuleException()
    {
        var auction = MakeAuction(currentPrice: 100m);
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);

        var act = () => _sut.PlaceBidAsync(1, new PlaceBidDto { Amount = 100m, BidderId = 2 });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*higher*");
    }

    [Fact]
    public async Task PlaceBidAsync_OwnerBidsOnOwnAuction_ThrowsBusinessRuleException()
    {
        var auction = MakeAuction(ownerId: 1);
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);

        var act = () => _sut.PlaceBidAsync(1, new PlaceBidDto { Amount = 200m, BidderId = 1 });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*owner*");
    }

    [Fact]
    public async Task PlaceBidAsync_EndedAuction_ThrowsBusinessRuleException()
    {
        var auction = MakeAuction(status: AuctionStatus.Ended);
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);

        var act = () => _sut.PlaceBidAsync(1, new PlaceBidDto { Amount = 200m, BidderId = 2 });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*ended*");
    }

    [Fact]
    public async Task PlaceBidAsync_CancelledAuction_ThrowsBusinessRuleException()
    {
        var auction = MakeAuction(status: AuctionStatus.Cancelled);
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);

        var act = () => _sut.PlaceBidAsync(1, new PlaceBidDto { Amount = 200m, BidderId = 2 });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*cancelled*");
    }

    [Fact]
    public async Task PlaceBidAsync_AuctionExpiredByDate_ThrowsBusinessRuleException()
    {
        var auction = MakeAuction(endDate: DateTime.UtcNow.AddDays(-1));
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);
        _auctionRepo.Setup(r => r.UpdateAsync(It.IsAny<Auction>())).ReturnsAsync((Auction a) => a);

        var act = () => _sut.PlaceBidAsync(1, new PlaceBidDto { Amount = 200m, BidderId = 2 });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*ended*");
    }

    [Fact]
    public async Task PlaceBidAsync_NonExistingAuction_ThrowsNotFoundException()
    {
        _auctionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Auction?)null);

        var act = () => _sut.PlaceBidAsync(99, new PlaceBidDto { Amount = 100m, BidderId = 2 });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task PlaceBidAsync_NonExistingBidder_ThrowsNotFoundException()
    {
        var auction = MakeAuction();
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);
        _userRepo.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        var act = () => _sut.PlaceBidAsync(1, new PlaceBidDto { Amount = 200m, BidderId = 99 });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByAuctionAsync_ExistingAuction_ReturnsBids()
    {
        _auctionRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _bidRepo.Setup(r => r.GetByAuctionIdAsync(1)).ReturnsAsync(new List<Bid>
        {
            new() { Id = 1, Amount = 110m, AuctionId = 1, BidderId = 2, PlacedAt = DateTime.UtcNow, Bidder = new User { Username = "user2" } },
            new() { Id = 2, Amount = 130m, AuctionId = 1, BidderId = 3, PlacedAt = DateTime.UtcNow, Bidder = new User { Username = "user3" } }
        });

        var result = (await _sut.GetByAuctionAsync(1)).ToList();

        result.Should().HaveCount(2);
        result[0].Amount.Should().Be(110m);
        result[1].Amount.Should().Be(130m);
    }

    [Fact]
    public async Task GetByAuctionAsync_NonExistingAuction_ThrowsNotFoundException()
    {
        _auctionRepo.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        var act = () => _sut.GetByAuctionAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Auction MakeAuction(
        decimal currentPrice   = 100m,
        int     ownerId        = 1,
        AuctionStatus status   = AuctionStatus.Active,
        DateTime? endDate      = null) => new()
    {
        Id            = 1,
        Name          = "Test",
        CurrentPrice  = currentPrice,
        StartingPrice = 50m,
        OwnerId       = ownerId,
        Status        = status,
        StartDate     = DateTime.UtcNow.AddDays(-1),
        EndDate       = endDate ?? DateTime.UtcNow.AddDays(7),
        Owner         = new User { Id = ownerId, Username = "owner" }
    };
}

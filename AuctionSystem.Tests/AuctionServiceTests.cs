using AuctionSystem.API.DTOs.Auction;
using AuctionSystem.API.Exceptions;
using AuctionSystem.API.Models;
using AuctionSystem.API.Repositories;
using AuctionSystem.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AuctionSystem.Tests;

public class AuctionServiceTests
{
    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IUserRepository>    _userRepo    = new();
    private readonly AuctionService           _sut;

    public AuctionServiceTests()
    {
        _sut = new AuctionService(_auctionRepo.Object, _userRepo.Object,
            NullLogger<AuctionService>.Instance);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsAuctionDto()
    {
        var auction = MakeAuction(1);
        _auctionRepo.Setup(r => r.GetByIdWithBidsAsync(1)).ReturnsAsync(auction);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Auction");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        _auctionRepo.Setup(r => r.GetByIdWithBidsAsync(99)).ReturnsAsync((Auction?)null);

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*99*");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedAuction()
    {
        var dto = new CreateAuctionDto
        {
            Name          = "New Auction",
            Description   = "Desc",
            Category      = "Electronics",
            StartingPrice = 100m,
            StartDate     = DateTime.UtcNow,
            EndDate       = DateTime.UtcNow.AddDays(7),
            OwnerId       = 1
        };
        _userRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _auctionRepo.Setup(r => r.AddAsync(It.IsAny<Auction>()))
            .ReturnsAsync((Auction a) => { a.Id = 5; a.Owner = new User { Username = "owner" }; return a; });

        var result = await _sut.CreateAsync(dto);

        result.Id.Should().Be(5);
        result.Name.Should().Be("New Auction");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_ThrowsValidationException()
    {
        var dto = new CreateAuctionDto
        {
            Name          = "Bad Auction",
            StartingPrice = 10m,
            StartDate     = DateTime.UtcNow.AddDays(5),
            EndDate       = DateTime.UtcNow.AddDays(1),
            OwnerId       = 1
        };

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*EndDate*");
    }

    [Fact]
    public async Task CreateAsync_NonExistingOwner_ThrowsNotFoundException()
    {
        var dto = new CreateAuctionDto
        {
            Name          = "Auction",
            StartingPrice = 10m,
            StartDate     = DateTime.UtcNow,
            EndDate       = DateTime.UtcNow.AddDays(3),
            OwnerId       = 999
        };
        _userRepo.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*999*");
    }

    [Fact]
    public async Task UpdateAsync_ActiveAuction_UpdatesSuccessfully()
    {
        var auction = MakeAuction(1);
        var dto     = new UpdateAuctionDto { Name = "Updated Name" };
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);
        _auctionRepo.Setup(r => r.UpdateAsync(It.IsAny<Auction>()))
            .ReturnsAsync((Auction a) => { a.Owner = new User { Username = "owner" }; return a; });

        var result = await _sut.UpdateAsync(1, dto);

        result.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_EndedAuction_ThrowsBusinessRuleException()
    {
        var auction = MakeAuction(1, AuctionStatus.Ended);
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);

        var act = () => _sut.UpdateAsync(1, new UpdateAuctionDto { Name = "X" });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*ended*");
    }

    [Fact]
    public async Task UpdateAsync_CancelledAuction_ThrowsBusinessRuleException()
    {
        var auction = MakeAuction(1, AuctionStatus.Cancelled);
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);

        var act = () => _sut.UpdateAsync(1, new UpdateAuctionDto { Name = "X" });

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task UpdateAsync_EndDateInPast_ThrowsValidationException()
    {
        var auction = MakeAuction(1);
        _auctionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(auction);
        var dto = new UpdateAuctionDto { EndDate = DateTime.UtcNow.AddDays(-1) };

        var act = () => _sut.UpdateAsync(1, dto);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*future*");
    }

    [Fact]
    public async Task DeleteAsync_ExistingAuction_CallsDelete()
    {
        _auctionRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _auctionRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(1);

        _auctionRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingAuction_ThrowsNotFoundException()
    {
        _auctionRepo.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        var act = () => _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var auctions = new List<Auction> { MakeAuction(1), MakeAuction(2) };
        _auctionRepo.Setup(r => r.GetAllAsync(It.IsAny<AuctionQueryDto>()))
            .ReturnsAsync((auctions, 2));

        var result = await _sut.GetAllAsync(new AuctionQueryDto());

        result.TotalCount.Should().Be(2);
        result.Data.Should().HaveCount(2);
    }

    private static Auction MakeAuction(int id, AuctionStatus status = AuctionStatus.Active) => new()
    {
        Id            = id,
        Name          = "Test Auction",
        Description   = "Desc",
        Category      = "Test",
        StartingPrice = 100m,
        CurrentPrice  = 100m,
        StartDate     = DateTime.UtcNow.AddDays(-1),
        EndDate       = DateTime.UtcNow.AddDays(7),
        Status        = status,
        OwnerId       = 1,
        Owner         = new User { Id = 1, Username = "owner" },
        Bids          = new List<Bid>()
    };
}

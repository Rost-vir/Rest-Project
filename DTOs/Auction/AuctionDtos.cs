using System.ComponentModel.DataAnnotations;
using AuctionSystem.API.Models;

namespace AuctionSystem.API.DTOs.Auction;

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class AuctionQueryDto
{
    public string? Category { get; set; }
    public AuctionStatus? Status { get; set; }
    public string? SortBy { get; set; }
    public string? Order { get; set; } = "asc";
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)]          public int PageSize { get; set; } = 10;
}

public class CreateAuctionDto
{
    [Required][StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;
    [Required][Range(0.01, double.MaxValue)]
    public decimal StartingPrice { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    [Required][Range(1, int.MaxValue)]
    public int OwnerId { get; set; }
}

public class UpdateAuctionDto
{
    [StringLength(200, MinimumLength = 2)] public string? Name { get; set; }
    [StringLength(1000)]                   public string? Description { get; set; }
    [StringLength(100)]                    public string? Category { get; set; }
    public DateTime?      EndDate { get; set; }
    public AuctionStatus? Status  { get; set; }
}

public class PlaceBidDto
{
    [Required][Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    [Required][Range(1, int.MaxValue)]
    public int BidderId { get; set; }
}

public class AuctionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public int BidCount { get; set; }
}

public class BidDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PlacedAt { get; set; }
    public int AuctionId { get; set; }
    public int BidderId { get; set; }
    public string BidderUsername { get; set; } = string.Empty;
}

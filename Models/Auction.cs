namespace AuctionSystem.API.Models;

public enum AuctionStatus { Active, Ended, Cancelled }

public class Auction
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Active;
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}

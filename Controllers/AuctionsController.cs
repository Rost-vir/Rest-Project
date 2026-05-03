using AuctionSystem.API.DTOs.Auction;
using AuctionSystem.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

// Manages auctions and bids
[ApiController]
[Route("api/auctions")]
[Produces("application/json")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly IBidService     _bidService;

    public AuctionsController(IAuctionService auctionService, IBidService bidService)
    {
        _auctionService = auctionService;
        _bidService     = bidService;
    }

    // Auctions
    // GET /api/auctions
    // GET /api/auctions?category=Electronics&amp;status=Active
    // GET /api/auctions?sortBy=currentPrice&amp;order=desc
    // GET /api/auctions?page=2&amp;pageSize=5
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuctionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AuctionQueryDto query)
    {
        var result = await _auctionService.GetAllAsync(query);
        return Ok(result);
    }

    // Returns a single auction by ID, including bid history.
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AuctionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var auction = await _auctionService.GetByIdAsync(id);
        return Ok(auction);
    }

    // Creates a new auction listing.
    [HttpPost]
    [ProducesResponseType(typeof(AuctionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAuctionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _auctionService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // Updates an existing auction. Not allowed when the auction has ended or been cancelled.
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AuctionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAuctionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _auctionService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    // Deletes an auction permanently.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _auctionService.DeleteAsync(id);
        return NoContent();
    }
    // Returns the full bid history for a specific auction, newest first.
    [HttpGet("{id:int}/bids")]
    [ProducesResponseType(typeof(IEnumerable<BidDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBids(int id)
    {
        var bids = await _bidService.GetByAuctionAsync(id);
        return Ok(bids);
    }
    // Places a new bid on an auction
    [HttpPost("{id:int}/bids")]
    [ProducesResponseType(typeof(BidDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PlaceBid(int id, [FromBody] PlaceBidDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var bid = await _bidService.PlaceBidAsync(id, dto);
        return CreatedAtAction(nameof(GetBids), new { id }, bid);
    }
}

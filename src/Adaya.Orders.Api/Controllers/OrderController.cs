using Adaya.Orders.Api.Models;
using Adaya.Orders.Application.Common;
using Adaya.Orders.Application.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Adaya.Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
[ProducesResponseType<ApiError>(StatusCodes.Status500InternalServerError)]
public class OrderController(IOrderService service) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Buat order")]
    [EndpointDescription("Mengurangi stock secara atomic. Request ulang dengan Idempotency-Key yang sama mengembalikan order yang sudah ada (200).")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(idempotencyKey ?? "", request, ct);

        if (result.IsReplay)
        {
            Response.Headers["Idempotency-Replayed"] = "true";
            return Ok(result.Order);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Order.Id }, result.Order);
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Ambil detail order")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Get(int id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    [HttpGet]
    [EndpointSummary("List order dengan filter dan pagination")]
    [ProducesResponseType<PagedResult<OrderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OrderDto>>> List([FromQuery] ListOrdersQuery query, CancellationToken ct)
        => Ok(await service.ListAsync(query, ct));

    [HttpPatch("{id:int}/status")]
    [EndpointSummary("Ubah status order")]
    [EndpointDescription("Transisi valid: Pending→Confirmed|Cancelled, Confirmed→Shipped|Cancelled, Shipped→Delivered. Cancelled mengembalikan stock.")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
        => Ok(await service.UpdateStatusAsync(id, request.Status, ct));

    [HttpPost("{id:int}/cancel")]
    [EndpointSummary("Cancel order dan kembalikan stock")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancellationToken ct)
        => Ok(await service.CancelAsync(id, ct));
}
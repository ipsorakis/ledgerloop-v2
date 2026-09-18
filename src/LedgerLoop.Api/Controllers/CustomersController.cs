using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LedgerLoop.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly CustomerService _customers;

    public CustomersController(CustomerService customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponse>>> List(CancellationToken ct) =>
        Ok(await _customers.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Get(Guid id, CancellationToken ct)
    {
        var customer = await _customers.GetAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CustomerRequest request, CancellationToken ct)
    {
        var created = await _customers.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, [FromBody] CustomerRequest request, CancellationToken ct)
    {
        var updated = await _customers.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}

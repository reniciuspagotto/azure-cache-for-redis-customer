using System.Text.Json;
using Customer.Api.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Customer.Api.Controllers;

[ApiController]
[Route("api/customer")]
public class CustomerController : ControllerBase
{
    private readonly CustomerContext _customerContext;
    private readonly IDistributedCache _cache;
    
    public CustomerController(CustomerContext customerContext, IDistributedCache cache)
    {
        _customerContext = customerContext;
        _cache = cache;
    }

    [HttpPost]
    public async Task<IActionResult> Save(Entity.Customer customer)
    {
        await _customerContext.AddAsync(customer);
        await _customerContext.SaveChangesAsync();

        return Ok();
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Entity.Customer customer)
    {
        var customerDb = await _customerContext.Customers.FirstOrDefaultAsync(p => p.Id == id);
        if (customerDb is not null)
        {
            customerDb.FullName = customer.FullName;
            customerDb.Email = customer.Email;
            
            _customerContext.Update(customerDb);
            await _customerContext.SaveChangesAsync();
            
            await _cache.RemoveAsync($"customer:{id}");
            return Ok();
        }

        return Ok("Customer not found");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customerCached = await _cache.GetStringAsync($"customer:{id}");

        if (customerCached is not null)
        {
            var customer = JsonSerializer.Deserialize<Entity.Customer>(customerCached);
            customer.Source = "cache";
            return Ok(customer);
        }
        
        var customerDb = await _customerContext.Customers.FirstOrDefaultAsync(p => p.Id == id);

        if (customerDb is not null)
        {
            await _cache.SetStringAsync($"customer:{customerDb.Id}", JsonSerializer.Serialize(customerDb), 
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                });
            
            customerDb.Source = "database";
            return Ok(customerDb);
        }
            
        return Ok("Customer not found");
    }
}
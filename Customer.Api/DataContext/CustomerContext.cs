using Microsoft.EntityFrameworkCore;

namespace Customer.Api.Repository;

public class CustomerContext : DbContext
{
    public CustomerContext(DbContextOptions<CustomerContext> options) : base(options)
    { }
    
    public DbSet<Entity.Customer> Customers { get; set; }
}
using Microsoft.EntityFrameworkCore;
using SIG.BeautyDesk.Api.Contracts;
using SIG.BeautyDesk.Core.Entities;
using SIG.BeautyDesk.Data;

namespace SIG.BeautyDesk.Api.Services;

public sealed class CustomerService(BeautyDeskDbContext dbContext)
{
    public async Task<CustomerResponse> IdentifyOrCreateAsync(
        IdentifyOrCreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Customers
            .SingleOrDefaultAsync(x => x.Phone == request.Phone, cancellationToken);

        if (existing is not null)
        {
            existing.Name = request.Name;
            existing.Email = request.Email;
            existing.Notes = request.Notes;
            existing.ConsentMarketing = request.ConsentMarketing;
            existing.ConsentSMS = request.ConsentSMS;
            existing.PatchTestExpiry = request.PatchTestExpiry;

            await dbContext.SaveChangesAsync(cancellationToken);
            return ToResponse(existing);
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Notes = request.Notes,
            ConsentMarketing = request.ConsentMarketing,
            ConsentSMS = request.ConsentSMS,
            PatchTestExpiry = request.PatchTestExpiry
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    private static CustomerResponse ToResponse(Customer customer) =>
        new()
        {
            CustomerId = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            ConsentMarketing = customer.ConsentMarketing,
            ConsentSMS = customer.ConsentSMS,
            PatchTestExpiry = customer.PatchTestExpiry
        };
}

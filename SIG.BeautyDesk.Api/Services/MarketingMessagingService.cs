using Microsoft.EntityFrameworkCore;
using SIG.BeautyDesk.Api.Contracts;
using SIG.BeautyDesk.Data;

namespace SIG.BeautyDesk.Api.Services;

public sealed class MarketingMessagingService(BeautyDeskDbContext dbContext)
{
    public async Task<MarketingSmsResponse> SendMarketingSmsAsync(
        MarketingSmsRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(x => x.Id == request.CustomerId, cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException($"Customer '{request.CustomerId}' was not found.");
        }

        if (!customer.ConsentSMS || !customer.ConsentMarketing)
        {
            return new MarketingSmsResponse
            {
                Allowed = false,
                Result = "Blocked due to missing ConsentSMS and/or ConsentMarketing."
            };
        }

        return new MarketingSmsResponse
        {
            Allowed = true,
            Result = "Consent validated. Message queued for downstream sender."
        };
    }
}

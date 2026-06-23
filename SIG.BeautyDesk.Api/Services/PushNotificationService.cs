using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIG.BeautyDesk.Api.Contracts;
using SIG.BeautyDesk.Core.Entities;
using SIG.BeautyDesk.Data;

namespace SIG.BeautyDesk.Api.Services;

public sealed class PushNotificationService(
    BeautyDeskDbContext dbContext,
    IOptions<PushNotificationOptions> options,
    ILogger<PushNotificationService> logger)
{
    public async Task RegisterDeviceAsync(RegisterPushDeviceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PushToken))
        {
            throw new InvalidOperationException("Push token is required.");
        }

        var staffExists = await dbContext.Staff.AnyAsync(x => x.Id == request.StaffId, cancellationToken);
        if (!staffExists)
        {
            throw new InvalidOperationException($"Staff '{request.StaffId}' was not found.");
        }

        var existing = await dbContext.StaffDevices.SingleOrDefaultAsync(
            x => x.StaffId == request.StaffId && x.PushToken == request.PushToken,
            cancellationToken);

        if (existing is null)
        {
            dbContext.StaffDevices.Add(new StaffDevice
            {
                Id = Guid.NewGuid(),
                StaffId = request.StaffId,
                Platform = request.Platform,
                PushToken = request.PushToken,
                Enabled = true,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Platform = request.Platform;
            existing.Enabled = true;
            existing.UpdatedUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> NotifyStaffAsync(
        Guid staffId,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var enabledDevices = await dbContext.StaffDevices
            .AsNoTracking()
            .Where(x => x.StaffId == staffId && x.Enabled)
            .ToListAsync(cancellationToken);

        if (!options.Value.Enabled || enabledDevices.Count == 0)
        {
            return 0;
        }

        foreach (var device in enabledDevices)
        {
            logger.LogInformation(
                "Push notification dispatched to {Platform} device for staff {StaffId}: {Title} | {Body} | token={TokenPrefix}",
                device.Platform,
                staffId,
                title,
                body,
                device.PushToken.Length >= 8 ? device.PushToken[..8] : device.PushToken);
        }

        return enabledDevices.Count;
    }
}

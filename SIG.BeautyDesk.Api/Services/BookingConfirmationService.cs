using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIG.BeautyDesk.Core.Entities;
using SIG.BeautyDesk.Data;

namespace SIG.BeautyDesk.Api.Services;

public sealed class BookingConfirmationService(
    BeautyDeskDbContext dbContext,
    TwilioSmsGateway smsGateway)
{
    public async Task<string> SendConfirmationAsync(
        Booking booking,
        Customer customer,
        Service service,
        IReadOnlyList<BookingSegment> segments,
        CancellationToken cancellationToken)
    {
        var firstSegment = segments.OrderBy(x => x.StartUtc).FirstOrDefault()
            ?? throw new InvalidOperationException("Booking must include at least one segment.");

        var message =
            $"BeautyDesk booking confirmed for {customer.Name}: {service.Name} at {firstSegment.StartUtc:yyyy-MM-dd HH:mm} UTC. " +
            $"If this is incorrect, reply to contact reception.";

        await smsGateway.SendAsync(customer.Phone, message, cancellationToken);

        var reminders = ParseReminders(booking.RemindersSent);
        reminders.Add(new ReminderEvent
        {
            SentUtc = DateTime.UtcNow,
            Channel = "sms",
            Kind = "booking_confirmation",
            Detail = "Twilio confirmation sent."
        });

        booking.RemindersSent = JsonSerializer.Serialize(reminders);
        dbContext.Bookings.Update(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
        return "sent";
    }

    private static List<ReminderEvent> ParseReminders(string? remindersSent)
    {
        if (string.IsNullOrWhiteSpace(remindersSent))
        {
            return [];
        }

        var parsed = JsonSerializer.Deserialize<List<ReminderEvent>>(remindersSent);
        return parsed ?? [];
    }

    private sealed class ReminderEvent
    {
        public DateTime SentUtc { get; init; }

        public required string Channel { get; init; }

        public required string Kind { get; init; }

        public required string Detail { get; init; }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIG.BeautyDesk.Api.Contracts;
using SIG.BeautyDesk.Api.Hubs;
using SIG.BeautyDesk.Api.Security;
using SIG.BeautyDesk.Api.Services;
using SIG.BeautyDesk.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.Configure<N8nAuthOptions>(builder.Configuration.GetSection(N8nAuthOptions.SectionName));
builder.Services.Configure<CallLogRetentionOptions>(builder.Configuration.GetSection(CallLogRetentionOptions.SectionName));
builder.Services.Configure<TwilioSmsOptions>(builder.Configuration.GetSection(TwilioSmsOptions.SectionName));
builder.Services.Configure<PushNotificationOptions>(builder.Configuration.GetSection(PushNotificationOptions.SectionName));

builder.Services.AddDbContext<BeautyDeskDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BeautyDesk")
        ?? "Server=(localdb)\\MSSQLLocalDB;Database=BeautyDesk;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"));

builder.Services.AddScoped<BookingEngineService>();
builder.Services.AddScoped<BookingConfirmationService>();
builder.Services.AddScoped<N8nVoiceOrchestrationService>();
builder.Services.AddScoped<StaffAgendaService>();
builder.Services.AddScoped<PushNotificationService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<EnquiryService>();
builder.Services.AddScoped<MarketingMessagingService>();
builder.Services.AddHttpClient<TwilioSmsGateway>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<N8nWebhookAuthMiddleware>();

app.MapPost("/api/enquiries", async (
        [FromBody] CreateEnquiryRequest request,
        EnquiryService enquiryService,
        CancellationToken cancellationToken) =>
    {
        var enquiry = await enquiryService.CreateEnquiryAsync(request, cancellationToken);
        return Results.Ok(enquiry);
    })
    .WithName("CreateEnquiry");

app.MapPost("/api/enquiries/{enquiryId:guid}/escalate", async (
        Guid enquiryId,
        [FromBody] EscalateToHumanRequest request,
        EnquiryService enquiryService,
        CancellationToken cancellationToken) =>
    {
        var response = await enquiryService.EscalateToHumanAsync(enquiryId, request.Reason, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("EscalateEnquiry");

app.MapPost("/api/contracts/GetAvailability", async (
        [FromBody] GetAvailabilityRequest request,
        BookingEngineService bookingEngineService,
        CancellationToken cancellationToken) =>
    {
        var response = await bookingEngineService.GetAvailabilityAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("GetAvailability");

app.MapPost("/api/contracts/IdentifyOrCreateCustomer", async (
        [FromBody] IdentifyOrCreateCustomerRequest request,
        CustomerService customerService,
        CancellationToken cancellationToken) =>
    {
        var response = await customerService.IdentifyOrCreateAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("IdentifyOrCreateCustomer");

app.MapPost("/api/contracts/CreateBooking", async (
        [FromBody] CreateBookingRequest request,
        BookingEngineService bookingEngineService,
        CancellationToken cancellationToken) =>
    {
        var response = await bookingEngineService.CreateBookingAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("CreateBooking");

app.MapPost("/api/contracts/EscalateToHuman", async (
        [FromBody] EscalateToHumanRequest request,
        EnquiryService enquiryService,
        CancellationToken cancellationToken) =>
    {
        var response = await enquiryService.EscalateToHumanAsync(request.EnquiryId, request.Reason, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("EscalateToHuman");

app.MapGet("/api/staff/{staffId:guid}/agenda", async (
        Guid staffId,
        DateTime dayUtc,
        StaffAgendaService staffAgendaService,
        CancellationToken cancellationToken) =>
    {
        var response = await staffAgendaService.GetAgendaAsync(staffId, dayUtc, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("GetStaffAgenda");

app.MapPatch("/api/bookings/{bookingId:guid}/status", async (
        Guid bookingId,
        [FromBody] UpdateBookingStatusRequest request,
        StaffAgendaService staffAgendaService,
        CancellationToken cancellationToken) =>
    {
        await staffAgendaService.UpdateStatusAsync(bookingId, request.Status, cancellationToken);
        return Results.NoContent();
    })
    .WithName("UpdateBookingStatus");

app.MapPost("/api/push/register", async (
        [FromBody] RegisterPushDeviceRequest request,
        PushNotificationService pushNotificationService,
        CancellationToken cancellationToken) =>
    {
        await pushNotificationService.RegisterDeviceAsync(request, cancellationToken);
        return Results.NoContent();
    })
    .WithName("RegisterPushDevice");

app.MapPost("/api/communications/marketing-sms", async (
        [FromBody] MarketingSmsRequest request,
        MarketingMessagingService messagingService,
        CancellationToken cancellationToken) =>
    {
        var response = await messagingService.SendMarketingSmsAsync(request, cancellationToken);
        return response.Allowed ? Results.Ok(response) : Results.Forbid();
    })
    .WithName("SendMarketingSms");

app.MapPost("/api/n8n/GetAvailability", async (
        [FromBody] GetAvailabilityRequest request,
        BookingEngineService bookingEngineService,
        CancellationToken cancellationToken) =>
    {
        var response = await bookingEngineService.GetAvailabilityAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("N8nGetAvailability");

app.MapPost("/api/n8n/IdentifyOrCreateCustomer", async (
        [FromBody] IdentifyOrCreateCustomerRequest request,
        CustomerService customerService,
        CancellationToken cancellationToken) =>
    {
        var response = await customerService.IdentifyOrCreateAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("N8nIdentifyOrCreateCustomer");

app.MapPost("/api/n8n/CreateBooking", async (
        [FromBody] CreateBookingRequest request,
        BookingEngineService bookingEngineService,
        CancellationToken cancellationToken) =>
    {
        var response = await bookingEngineService.CreateBookingAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("N8nCreateBooking");

app.MapPost("/api/n8n/EscalateToHuman", async (
        [FromBody] EscalateToHumanRequest request,
        EnquiryService enquiryService,
        CancellationToken cancellationToken) =>
    {
        var response = await enquiryService.EscalateToHumanAsync(request.EnquiryId, request.Reason, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("N8nEscalateToHuman");

app.MapPost("/api/n8n/voice/intents", async (
        [FromBody] N8nVoiceIntentRequest request,
        N8nVoiceOrchestrationService orchestrationService,
        CancellationToken cancellationToken) =>
    {
        var response = await orchestrationService.HandleIntentAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("N8nVoiceIntent");

app.MapHub<ReceptionHub>("/hubs/reception");

app.Run();

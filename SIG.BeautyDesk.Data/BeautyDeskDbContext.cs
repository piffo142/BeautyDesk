using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SIG.BeautyDesk.Core.Entities;

namespace SIG.BeautyDesk.Data;

public sealed class BeautyDeskDbContext(DbContextOptions<BeautyDeskDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Enquiry> Enquiries => Set<Enquiry>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<Staff> Staff => Set<Staff>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingSegment> BookingSegments => Set<BookingSegment>();

    public DbSet<CallLog> CallLogs => Set<CallLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCustomer(modelBuilder);
        ConfigureEnquiry(modelBuilder);
        ConfigureService(modelBuilder);
        ConfigureStaff(modelBuilder);
        ConfigureResource(modelBuilder);
        ConfigureBooking(modelBuilder);
        ConfigureBookingSegment(modelBuilder);
        ConfigureCallLog(modelBuilder);
        ConfigureUtcDateTimes(modelBuilder);
    }

    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Customer>();
        entity.ToTable(
            "Customers",
            table => table.HasCheckConstraint(
                "CK_Customers_Phone_E164",
                "Phone LIKE '+[1-9]%' AND LEN(Phone) BETWEEN 8 AND 20"));
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        entity.Property(x => x.Email).HasMaxLength(320);
        entity.Property(x => x.PreferredTherapistId);
        entity.Property(x => x.ConsentMarketing).HasDefaultValue(false);
        entity.Property(x => x.ConsentSMS).HasDefaultValue(false);
        entity.Property(x => x.PatchTestExpiry);
        entity.HasIndex(x => x.Phone).IsUnique();
        entity.HasOne(x => x.PreferredTherapist)
            .WithMany(x => x.PreferredByCustomers)
            .HasForeignKey(x => x.PreferredTherapistId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureEnquiry(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Enquiry>();
        entity.ToTable("Enquiries");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(24).IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        entity.Property(x => x.InboundCallSid).HasMaxLength(128);
        entity.Property(x => x.AssignedToUserId).HasMaxLength(128);
        entity.Property(x => x.Tags).HasColumnType("nvarchar(max)");
        entity.Property(x => x.CreatedUtc).IsRequired();
        entity.HasIndex(x => x.CustomerId);
        entity.HasOne(x => x.Customer)
            .WithMany(x => x.Enquiries)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureService(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Service>();
        entity.ToTable("Services");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Price).HasColumnType("decimal(10,2)");
        entity.Property(x => x.RequiredSkillTag).HasMaxLength(100);
    }

    private static void ConfigureStaff(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Staff>();
        entity.ToTable("Staff");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.WorkingHoursJson).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.SkillTags).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.MaxConcurrentBookings).HasDefaultValue(1);
    }

    private static void ConfigureResource(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Resource>();
        entity.ToTable("Resources");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
    }

    private static void ConfigureBooking(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Booking>();
        entity.ToTable("Bookings");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        entity.Property(x => x.DepositTakenVia).HasMaxLength(50);
        entity.Property(x => x.RemindersSent).HasColumnType("nvarchar(max)");
        entity.HasIndex(x => x.CustomerId);
        entity.HasIndex(x => x.StaffId);
        entity.HasIndex(x => x.ResourceId);
        entity.HasIndex(x => x.EnquiryId);
        entity.HasOne(x => x.Customer)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Service)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Staff)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Resource)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Enquiry)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.EnquiryId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureBookingSegment(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BookingSegment>();
        entity.ToTable("BookingSegments");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.StartUtc).IsRequired();
        entity.Property(x => x.EndUtc).IsRequired();
        entity.HasIndex(x => new { x.BookingId, x.StartUtc, x.EndUtc });
        entity.HasIndex(x => new { x.StaffOccupied, x.StartUtc, x.EndUtc });
        entity.HasIndex(x => new { x.ResourceOccupied, x.StartUtc, x.EndUtc });
        entity.HasOne(x => x.Booking)
            .WithMany(x => x.Segments)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureCallLog(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CallLog>();
        entity.ToTable("CallLogs");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.CallSid).HasMaxLength(128).IsRequired();
        entity.Property(x => x.FromNumber).HasMaxLength(20).IsRequired();
        entity.Property(x => x.RecordingUrl).HasMaxLength(2048);
        entity.Property(x => x.N8nWorkflowExecutionId).HasMaxLength(128);
        entity.HasIndex(x => x.CallSid).IsUnique();
    }

    private static void ConfigureUtcDateTimes(ModelBuilder modelBuilder)
    {
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            value => value.HasValue
                ? (value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime())
                : value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                    property.SetValueComparer(new ValueComparer<DateTime>(
                        (a, b) => a.ToUniversalTime() == b.ToUniversalTime(),
                        value => value.ToUniversalTime().GetHashCode(),
                        value => DateTime.SpecifyKind(value, DateTimeKind.Utc)));
                }

                if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                    property.SetValueComparer(new ValueComparer<DateTime?>(
                        (a, b) =>
                            (!a.HasValue && !b.HasValue) ||
                            (a.HasValue && b.HasValue && a.Value.ToUniversalTime() == b.Value.ToUniversalTime()),
                        value => value.HasValue ? value.Value.ToUniversalTime().GetHashCode() : 0,
                        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value));
                }
            }
        }
    }
}

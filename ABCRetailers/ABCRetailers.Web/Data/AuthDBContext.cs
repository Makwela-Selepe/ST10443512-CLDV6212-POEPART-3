using ABCRetailers.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Web.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Cart> Cart { get; set; }
        public DbSet<PaymentProofEntity> PaymentProofs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Email)
                    .HasMaxLength(255);

                entity.Property(e => e.DeliveryAddress)
                    .HasMaxLength(500);

                entity.Property(e => e.Phone)
                    .HasMaxLength(50);

                entity.Property(e => e.FirstName)
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .HasMaxLength(100);

                entity.Property(e => e.DateCreated)
                    .IsRequired();

                entity.Property(e => e.LastLogin)
                    .IsRequired(false);

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);
            });

            // Seed test data with more details
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "admin123",
                    Role = "Admin",
                    Email = "admin@abcretailers.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    Phone = "+1-555-0100",
                    DateCreated = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Id = 2,
                    Username = "customer",
                    PasswordHash = "customer123",
                    Role = "Customer",
                    Email = "customer@abcretailers.com",
                    FirstName = "John",
                    LastName = "Smith",
                    Phone = "+1-555-0101",
                    DeliveryAddress = "123 Main Street, New York, NY 10001",
                    DateCreated = DateTime.UtcNow.AddDays(-30),
                    LastLogin = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                },
                new User
                {
                    Id = 3,
                    Username = "sarahj",
                    PasswordHash = "sarah123",
                    Role = "Customer",
                    Email = "sarah.johnson@email.com",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Phone = "+1-555-0102",
                    DeliveryAddress = "456 Oak Avenue, Los Angeles, CA 90210",
                    DateCreated = DateTime.UtcNow.AddDays(-15),
                    LastLogin = DateTime.UtcNow.AddHours(-2),
                    IsActive = true
                },
                new User
                {
                    Id = 4,
                    Username = "mikeb",
                    PasswordHash = "mike123",
                    Role = "Customer",
                    Email = "mike.brown@email.com",
                    FirstName = "Mike",
                    LastName = "Brown",
                    Phone = "+1-555-0103",
                    DeliveryAddress = "789 Pine Road, Chicago, IL 60601",
                    DateCreated = DateTime.UtcNow.AddDays(-7),
                    LastLogin = DateTime.UtcNow.AddDays(-3),
                    IsActive = true
                },
                new User
                {
                    Id = 5,
                    Username = "admin2",
                    PasswordHash = "admin456",
                    Role = "Admin",
                    Email = "admin2@abcretailers.com",
                    FirstName = "Jane",
                    LastName = "Wilson",
                    Phone = "+1-555-0104",
                    DateCreated = DateTime.UtcNow.AddDays(-60),
                    LastLogin = DateTime.UtcNow.AddHours(-5),
                    IsActive = true
                }
            );

            // Cart entity configuration (keep existing)
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("Cart");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CustomerUsername)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ProductId)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            // PaymentProof entity configuration (keep existing)
            // PaymentProof entity configuration
            // PaymentProof entity configuration
            modelBuilder.Entity<PaymentProofEntity>(entity =>
            {
                entity.ToTable("PaymentProofs");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.BlobName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.BlobUri)
                    .IsRequired()
                    .HasMaxLength(1000);
            });


        }
    }
}
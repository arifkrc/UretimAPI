using Microsoft.EntityFrameworkCore;
using UretimAPI.Entities;

namespace UretimAPI.Data
{
    public class UretimDbContext : DbContext
    {
        public UretimDbContext(DbContextOptions<UretimDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Product> Products { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<CycleTime> CycleTimes { get; set; }
        public DbSet<ProductionTrackingForm> ProductionTrackingForms { get; set; }
        public DbSet<Packing> Packings { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product Configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.ProductCode).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.Type).IsRequired().HasMaxLength(100);
                
                // LastOperation relationship
                entity.HasOne(p => p.LastOperation)
                      .WithMany(o => o.ProductsWithLastOperation)
                      .HasForeignKey(p => p.LastOperationId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Operation Configuration
            modelBuilder.Entity<Operation>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
                entity.Property(o => o.ShortCode).IsRequired().HasMaxLength(20);
            });

            // CycleTime Configuration
            modelBuilder.Entity<CycleTime>(entity =>
            {
                entity.HasKey(c => c.Id);
                
                // Product relationship
                entity.HasOne(c => c.Product)
                      .WithMany(p => p.CycleTimes)
                      .HasForeignKey(c => c.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // Operation relationship
                entity.HasOne(c => c.Operation)
                      .WithMany(o => o.CycleTimes)
                      .HasForeignKey(c => c.OperationId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // Unique constraint for ProductId-OperationId combination (only for active records)
                entity.HasIndex(c => new { c.ProductId, c.OperationId })
                      .IsUnique()
                      .HasFilter("[IsActive] = 1")
                      .HasDatabaseName("IX_CycleTime_ProductId_OperationId_Unique");
            });

            // ProductionTrackingForm Configuration
            modelBuilder.Entity<ProductionTrackingForm>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Shift).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Line).HasMaxLength(100);
                entity.Property(p => p.ShiftSupervisor).HasMaxLength(100);
                entity.Property(p => p.Machine).HasMaxLength(100);
                entity.Property(p => p.OperatorName).HasMaxLength(100);
                entity.Property(p => p.SectionSupervisor).HasMaxLength(100);
                entity.Property(p => p.ProductCode).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Operation).IsRequired().HasMaxLength(100);
                
                // Product relationship
                entity.HasOne(p => p.Product)
                      .WithMany(pr => pr.ProductionTrackingForms)
                      .HasPrincipalKey(pr => pr.ProductCode)
                      .HasForeignKey(p => p.ProductCode)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Packing Configuration
            modelBuilder.Entity<Packing>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Shift).HasMaxLength(50);
                entity.Property(p => p.Supervisor).HasMaxLength(100);
                entity.Property(p => p.ProductCode).IsRequired().HasMaxLength(50);
                entity.Property(p => p.ExplodedFrom).HasMaxLength(100);
                entity.Property(p => p.ExplodingTo).HasMaxLength(100);
                
                // Product relationship
                entity.HasOne(p => p.Product)
                      .WithMany(pr => pr.Packings)
                      .HasPrincipalKey(pr => pr.ProductCode)
                      .HasForeignKey(p => p.ProductCode)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Order Configuration (Standalone table)
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.OrderAddedDateTime).IsRequired().HasMaxLength(20);
                entity.Property(o => o.DocumentNo).IsRequired().HasMaxLength(100);
                entity.Property(o => o.Customer).IsRequired().HasMaxLength(200);
                entity.Property(o => o.ProductCode).IsRequired().HasMaxLength(50);
                entity.Property(o => o.Variants).HasMaxLength(200);
            });

            // Optimized Indexes for Reporting (500 daily queries)
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductCode)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.Type, p.IsActive })
                .HasDatabaseName("IX_Products_Type_IsActive");

            modelBuilder.Entity<Operation>()
                .HasIndex(o => o.ShortCode)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.DocumentNo);

            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.Customer, o.IsActive })
                .HasDatabaseName("IX_Orders_Customer_IsActive");

            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.OrderAddedDateTime, o.IsActive })
                .HasDatabaseName("IX_Orders_Week_IsActive");

            // Critical indexes for ProductionTrackingForm (most queried for reports)
            modelBuilder.Entity<ProductionTrackingForm>()
                .HasIndex(p => new { p.Date, p.IsActive })
                .HasDatabaseName("IX_PTF_Date_IsActive");

            modelBuilder.Entity<ProductionTrackingForm>()
                .HasIndex(p => new { p.ProductCode, p.Date, p.IsActive })
                .HasDatabaseName("IX_PTF_ProductCode_Date_IsActive");

            modelBuilder.Entity<ProductionTrackingForm>()
                .HasIndex(p => new { p.Shift, p.Date, p.IsActive })
                .HasDatabaseName("IX_PTF_Shift_Date_IsActive");

            modelBuilder.Entity<ProductionTrackingForm>()
                .HasIndex(p => new { p.Operation, p.Date, p.IsActive })
                .HasDatabaseName("IX_PTF_Operation_Date_IsActive");

            // Packing indexes for reporting
            modelBuilder.Entity<Packing>()
                .HasIndex(p => new { p.Date, p.IsActive })
                .HasDatabaseName("IX_Packing_Date_IsActive");

            modelBuilder.Entity<Packing>()
                .HasIndex(p => new { p.ProductCode, p.Date, p.IsActive })
                .HasDatabaseName("IX_Packing_ProductCode_Date_IsActive");
        }
    }
}
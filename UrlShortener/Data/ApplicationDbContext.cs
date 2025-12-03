using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Data.Models;

namespace UrlShortener.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UrlMapping> UrlMappings { get; set; }
        public DbSet<UrlClick> UrlClicks { get; set; }
        public DbSet<PricingPlan> PricingPlans { get; set; }
        public DbSet<PricingFeature> PricingFeatures { get; set; }
        public DbSet<FeatureCategory> FeatureCategories { get; set; }
        public DbSet<FAQ> FAQs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => new { u.Provider, u.ProviderId }).IsUnique();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // UrlMapping configuration
            modelBuilder.Entity<UrlMapping>(entity =>
            {
                entity.HasKey(u => u.ShortCode);
                entity.Property(u => u.ShortCode).HasMaxLength(10);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(u => u.ClickCount).HasDefaultValue(0);

                entity.HasIndex(u => u.UserId);
                entity.HasIndex(u => u.CreatedAt);
                entity.HasIndex(u => u.ExpiresAt);
            });

            // UrlClick configuration
            modelBuilder.Entity<UrlClick>(entity =>
            {
                entity.Property(c => c.ClickedAt).HasDefaultValueSql("NOW()");
                entity.HasIndex(c => c.ShortCode);
                entity.HasIndex(c => c.ClickedAt);
            });

            // Configure relationships
            modelBuilder.Entity<PricingPlan>()
                .HasMany(p => p.Features)
                .WithOne()
                .HasForeignKey("PricingPlanId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeatureCategory>()
                .HasMany(fc => fc.Features)
                .WithOne()
                .HasForeignKey("FeatureCategoryId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
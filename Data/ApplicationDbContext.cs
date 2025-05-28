using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SoftEng2025.Models;

namespace SoftEng2025.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Critic> Critics { get; set; }
        public DbSet<Entrepreneur> Entrepreneurs { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<RestaurantImage> RestaurantImages { get; set; }
        public DbSet<Review> Reviews { get; set; }

        // new sets
        public DbSet<FavoriteRestaurant> FavoriteRestaurants { get; set; }
        public DbSet<EntrepreneurNotification> EntrepreneurNotifications { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Map EF entities to your exact table names
            builder.Entity<FavoriteRestaurant>().ToTable("FavoriteRestaurant");
            builder.Entity<EntrepreneurNotification>().ToTable("EntrepreneurNotification");
            builder.Entity<AdminNotification>().ToTable("AdminNotification");

            // PostGIS mapping for Restaurant.Location
            builder.Entity<Restaurant>(e =>
            {
                e.Property(r => r.Location)
                 .HasColumnType("geography")
                 .IsRequired();
            });

            // composite key & relationships for favorites
            builder.Entity<FavoriteRestaurant>(e =>
            {
                // primary key
                e.HasKey(fr => new { fr.CriticId, fr.RestaurantId });

                // relationship back to Critic
                e.HasOne(fr => fr.Critic)
                 .WithMany(c => c.FavoriteRestaurants)
                 .HasForeignKey(fr => fr.CriticId);

                // relationship back to Restaurant
                e.HasOne(fr => fr.Restaurant)
                 .WithMany(r => r.FavoritedBy)
                 .HasForeignKey(fr => fr.RestaurantId);
            });

            // configure EntrepreneurNotification with its primary key
            builder.Entity<EntrepreneurNotification>(e =>
            {
                e.HasKey(n => n.NotificationId);

                e.HasOne(n => n.User)
                 .WithMany()
                 .HasForeignKey(n => n.UserId);

                e.HasOne(n => n.Review)
                 .WithMany()
                 .HasForeignKey(n => n.ReviewId);
            });

            // configure AdminNotification with its primary key
            builder.Entity<AdminNotification>(e =>
            {
                e.HasKey(n => n.NotificationId);

                e.HasOne(n => n.Restaurant)
                 .WithMany()
                 .HasForeignKey(n => n.RestaurantId);

                e.HasOne(n => n.Review)
                 .WithMany()
                 .HasForeignKey(n => n.ReviewId);
            });
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using Npgsql;
using TourPlanner.DataAccessLayer.Utils;

namespace TourPlanner.DataAccessLayer
{
    public class TourPlannerDbContext : DbContext
    {
        public TourPlannerDbContext(DbContextOptions<TourPlannerDbContext> options) : base(options)
        {
        }

        public DbSet<Tour> Tours { get; set; }
        public DbSet<Waypoint> Waypoints { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<TransportType>();

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("app_user");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.username).HasColumnName("username").IsRequired().HasMaxLength(50);
                entity.Property(e => e.email).HasColumnName("email").IsRequired().HasMaxLength(255);
                entity.Property(e => e.password_hash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
                entity.Property(e => e.created_at).HasColumnName("created_at").HasDefaultValueSql("NOW()");

                entity.HasIndex(e => e.username).IsUnique();
                entity.HasIndex(e => e.email).IsUnique();
            });

            modelBuilder.Entity<Tour>(entity =>
            {
                entity.ToTable("tour");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.userID).HasColumnName("user_id");
                entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.TransportType).HasConversion(
                    v => v.ToPgName(),
                    v => EnumExtensions.FromPgName<TransportType>(v)
                ).HasColumnName("transport_type");
                entity.Property(e => e.Distance_km).HasColumnName("distance_km");
                entity.Property(e => e.EstimatedTime).HasColumnName("estimated_time_min")
                    .HasConversion(
                        v => (int)v.TotalMinutes,
                        v => TimeSpan.FromMinutes(v)
                    );
                entity.Property(e => e.RouteInformation).HasColumnName("route_information"); // Note: Adding this to schema or mapping to map_image_path
                entity.Property(e => e.Popularity).HasColumnName("popularity").HasDefaultValue(0.0);
                entity.Property(e => e.ChildFriendliness).HasColumnName("child_friendliness").HasDefaultValue(0.0);
                entity.Property(e => e.CreationDate).HasColumnName("created_at").HasDefaultValueSql("NOW()");
                entity.Property(e => e.LastModifiedDate).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
                
                // Navigation property
                entity.HasMany(e => e.Waypoints)
                      .WithOne()
                      .HasForeignKey(w => w.TourId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Waypoint>(entity =>
            {
                entity.ToTable("tour_waypoint");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.TourId).HasColumnName("tour_id");
                entity.Property(e => e.OrderIndex).HasColumnName("order_index");
                entity.Property(e => e.Label).HasColumnName("label");
                entity.Property(e => e.Latitude).HasColumnName("latitude");
                entity.Property(e => e.Longitude).HasColumnName("longitude");
            });
        }
    }
}
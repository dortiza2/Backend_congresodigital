using Microsoft.EntityFrameworkCore;
using Congreso.Api.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Congreso.Api.Data;

public partial class CongresoDbContext : DbContext, IDataProtectionKeyContext
{
    public CongresoDbContext(DbContextOptions<CongresoDbContext> options) : base(options)
    {
    }

    public virtual DbSet<Activity> Activities { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<UserRole> UserRoles { get; set; }
    public virtual DbSet<UserInvitation> UserInvitations { get; set; }
    public virtual DbSet<Organization> Organizations { get; set; }
    public virtual DbSet<Speaker> Speakers { get; set; }
    public virtual DbSet<Team> Teams { get; set; }
    public virtual DbSet<TeamMember> TeamMembers { get; set; }
    public virtual DbSet<Enrollment> Enrollments { get; set; }
    public virtual DbSet<Winner> Winners { get; set; }
    public virtual DbSet<FaqItem> FaqItems { get; set; }
    public virtual DbSet<CheckInToken> CheckInTokens { get; set; }
    public virtual DbSet<QrJwtId> QrJwtIds { get; set; }
    public virtual DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    // Profile entities
    public virtual DbSet<StaffAccount> StaffAccounts { get; set; }
    public virtual DbSet<StudentAccount> StudentAccounts { get; set; }

    // Views
    public virtual DbSet<PublicActivityView> PublicActivities { get; set; }
    public virtual DbSet<UserEnrollmentView> UserEnrollments { get; set; }
    public virtual DbSet<PodiumByYearView> PodiumByYear { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure PostgreSQL enums
        modelBuilder.HasPostgresEnum<StaffRole>();
        
        // Configure composite keys
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<TeamMember>()
            .HasKey(tm => new { tm.TeamId, tm.UserId });

        // Configure unique indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email");

        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.UserId, e.ActivityId })
            .IsUnique()
            .HasDatabaseName("ux_enrollments_user_activity");

        modelBuilder.Entity<Winner>()
            .HasIndex(w => new { w.EditionYear, w.ActivityId, w.Place })
            .IsUnique()
            .HasDatabaseName("ux_winners_year_activity_place");

        modelBuilder.Entity<Activity>()
            .HasIndex(a => new { a.Published, a.StartTime })
            .HasDatabaseName("idx_activities_published_start");

        // Configure profile relationships (1:1 with User)
        modelBuilder.Entity<StaffAccount>()
            .HasOne(s => s.User)
            .WithOne(u => u.StaffAccount)
            .HasForeignKey<StaffAccount>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentAccount>()
            .HasOne(s => s.User)
            .WithOne(u => u.StudentAccount)
            .HasForeignKey<StudentAccount>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure profile indexes
        modelBuilder.Entity<StaffAccount>()
            .HasIndex(s => s.StaffRole)
            .HasDatabaseName("idx_staff_accounts_role");

        modelBuilder.Entity<StudentAccount>()
            .HasIndex(s => s.Carnet)
            .HasDatabaseName("idx_student_accounts_carnet");

        modelBuilder.Entity<StudentAccount>()
            .HasIndex(s => new { s.Career, s.CohortYear })
            .HasDatabaseName("idx_student_accounts_career_cohort");

        // Configure enrollment indexes for QR functionality
        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => e.QrCodeId)
            .HasDatabaseName("idx_enrollments_qr_code");

        // Configure ActivityType enum conversion
        modelBuilder.Entity<Activity>()
            .Property(a => a.ActivityType)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Configure StaffRole enum conversion
        modelBuilder.Entity<StaffAccount>()
            .Property(s => s.StaffRole)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Configure Winner UUID fields explicitly
        modelBuilder.Entity<Winner>()
            .Property(w => w.ActivityId)
            .HasColumnType("uuid");

        modelBuilder.Entity<Winner>()
            .Property(w => w.TeamId)
            .HasColumnType("uuid");

        modelBuilder.Entity<Winner>()
            .Property(w => w.UserId)
            .HasColumnType("uuid");

        // Configure views
        modelBuilder.Entity<PublicActivityView>()
            .ToView("vw_public_activities")
            .HasNoKey();
        
        // Configure PublicActivityView column mappings explicitly
        modelBuilder.Entity<PublicActivityView>()
            .Property(p => p.ActivityType)
            .HasColumnName("activity_type");

        modelBuilder.Entity<UserEnrollmentView>()
            .ToView("vw_user_enrollments")
            .HasNoKey();

        modelBuilder.Entity<PodiumByYearView>()
            .ToView("vw_podium_by_year")
            .HasNoKey();

        // Configure Speaker social column (PostgreSQL compatible)
        modelBuilder.Entity<Speaker>()
            .Property(p => p.Social)
            .HasColumnName("social")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        // Extra property removed from StudentAccount model

        // Configure StaffAccount Extra field (PostgreSQL compatible)
        modelBuilder.Entity<StaffAccount>()
            .Property(s => s.Extra)
            .HasColumnName("extra")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        // Configure QrJwtId indexes and relationships
        modelBuilder.Entity<QrJwtId>()
            .HasIndex(q => q.JwtId)
            .IsUnique()
            .HasDatabaseName("ux_qr_jwt_ids_jwt_id");

        modelBuilder.Entity<QrJwtId>()
            .HasIndex(q => q.ExpiresAt)
            .HasDatabaseName("idx_qr_jwt_ids_expires_at");

        modelBuilder.Entity<QrJwtId>()
            .HasIndex(q => new { q.UserId, q.ActivityId })
            .HasDatabaseName("idx_qr_jwt_ids_user_activity");

        // Configure all DateTime properties to use UTC
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
            }
        }

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
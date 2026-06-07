using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Entities;

namespace QualityControlSystem.Infrastructure;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Camera> Cameras { get; set; }

    public virtual DbSet<CheckNotification> CheckNotifications { get; set; }

    public virtual DbSet<Frame> Frames { get; set; }

    public virtual DbSet<FrameTestForm> FrameTestForms { get; set; }

    public virtual DbSet<FrameTestFormFrame> FrameTestFormFrames { get; set; }

    public virtual DbSet<FrameTestFormTemplate> FrameTestFormTemplates { get; set; }

    public virtual DbSet<MaterialType> MaterialTypes { get; set; }

    public virtual DbSet<MeasurementUnitClassifier> MeasurementUnitClassifiers { get; set; }

    public virtual DbSet<ProductionEquipment> ProductionEquipments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Sensor> Sensors { get; set; }

    public virtual DbSet<SensorTypeClassifier> SensorTypeClassifiers { get; set; }

    public virtual DbSet<Template> Templates { get; set; }

    public virtual DbSet<TestType> TestTypes { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<Workshop> Workshops { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = Environment.GetEnvironmentVariable("QUALITY_SYSTEM_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum("template_side", new[] { "front", "left", "right", "top", "back" });

        modelBuilder.Entity<Camera>(entity =>
        {
            entity.HasKey(e => e.CameraId).HasName("camera_pk");
            entity.HasIndex(e => e.InventoryNumber).IsUnique().HasDatabaseName("camera_inventory_number_unique");
            entity.HasOne(e => e.Workshop)
                .WithMany(e => e.Cameras)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("camera_workshop_fk");
        });

        modelBuilder.Entity<CheckNotification>(entity =>
        {
            entity.HasKey(e => e.CheckNotificationId).HasName("check_notification_pk");
            entity.HasOne(e => e.ProductionEquipment)
                .WithMany(e => e.CheckNotifications)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("check_notification_production_equipment_fk");
        });

        modelBuilder.Entity<Frame>(entity =>
        {
            entity.HasKey(e => e.FrameId).HasName("frame_pk");
            entity.HasOne(e => e.MaterialType)
                .WithMany(e => e.Frames)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("frame_material_type_fk");
            entity.HasOne(e => e.Workshop)
                .WithMany(e => e.Frames)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("frame_workshop_fk");
        });

        modelBuilder.Entity<FrameTestForm>(entity =>
        {
            entity.HasKey(e => e.FrameTestFormId).HasName("frame_test_form_pk");
            entity.HasOne(e => e.TestType)
                .WithMany(e => e.FrameTestForms)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("frame_test_form_test_type_fk");
        });

        modelBuilder.Entity<FrameTestFormFrame>(entity =>
        {
            entity.HasKey(e => e.FrameTestFormFrameId).HasName("frame_test_form_frame_pk");
            entity.HasIndex(e => new { e.FrameTestFormId, e.FrameId })
                .IsUnique()
                .HasDatabaseName("frame_test_form_frame_unique");
            entity.HasOne(e => e.Frame)
                .WithMany(e => e.FrameTestFormFrames)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("frame_test_form_frame_frame_fk");
            entity.HasOne(e => e.FrameTestForm)
                .WithMany(e => e.FrameTestFormFrames)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("frame_test_form_frame_form_fk");
        });

        modelBuilder.Entity<FrameTestFormTemplate>(entity =>
        {
            entity.HasKey(e => e.FrameTestFormTemplateId).HasName("frame_test_form_template_pk");
            entity.HasIndex(e => new { e.FrameTestFormId, e.TemplateId })
                .IsUnique()
                .HasDatabaseName("frame_test_form_template_unique");
            entity.HasOne(e => e.FrameTestForm)
                .WithMany(e => e.FrameTestFormTemplates)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("frame_test_form_template_form_fk");
            entity.HasOne(e => e.Template)
                .WithMany(e => e.FrameTestFormTemplates)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("frame_test_form_template_template_fk");
        });

        modelBuilder.Entity<MaterialType>(entity =>
        {
            entity.HasKey(e => e.MaterialTypeId).HasName("material_type_pk");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("material_type_code_unique");
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("material_type_name_unique");
        });

        modelBuilder.Entity<MeasurementUnitClassifier>(entity =>
        {
            entity.HasKey(e => e.MeasurementUnitId).HasName("measurement_unit_classifier_pk");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("measurement_unit_classifier_code_unique");
        });

        modelBuilder.Entity<ProductionEquipment>(entity =>
        {
            entity.HasKey(e => e.ProductionEquipmentId).HasName("production_equipment_pk");
            entity.HasIndex(e => e.InventoryNumber).IsUnique().HasDatabaseName("production_equipment_inventory_number_unique");
            entity.HasOne(e => e.Workshop)
                .WithMany(e => e.ProductionEquipments)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("production_equipment_workshop_fk");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pk");
            entity.HasIndex(e => e.RoleCode).IsUnique().HasDatabaseName("role_code_unique");
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("role_name_unique");
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.SensorId).HasName("sensor_pk");
            entity.HasIndex(e => e.InventoryNumber).IsUnique().HasDatabaseName("sensor_inventory_number_unique");
            entity.HasOne(e => e.MeasurementUnit)
                .WithMany(e => e.Sensors)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sensor_measurement_unit_fk");
            entity.HasOne(e => e.SensorType)
                .WithMany(e => e.Sensors)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sensor_type_fk");
            entity.HasOne(e => e.Workshop)
                .WithMany(e => e.Sensors)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sensor_workshop_fk");
        });

        modelBuilder.Entity<SensorTypeClassifier>(entity =>
        {
            entity.HasKey(e => e.SensorTypeId).HasName("sensor_type_classifier_pk");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("sensor_type_classifier_code_unique");
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("sensor_type_classifier_name_unique");
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("template_pk");
        });

        modelBuilder.Entity<TestType>(entity =>
        {
            entity.HasKey(e => e.TestTypeId).HasName("test_type_pk");
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("test_type_name_unique");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserProfileId).HasName("user_profile_pk");
            entity.HasIndex(e => e.PersonnelNumber).IsUnique().HasDatabaseName("user_profile_personnel_number_unique");
            entity.HasOne(e => e.Role)
                .WithMany(e => e.UserProfiles)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("user_profile_role_fk");
            entity.HasOne(e => e.Workshop)
                .WithMany(e => e.UserProfiles)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("user_profile_workshop_fk");
        });

        modelBuilder.Entity<Workshop>(entity =>
        {
            entity.HasKey(e => e.WorkshopId).HasName("workshop_pk");
            entity.HasIndex(e => e.Number).IsUnique().HasDatabaseName("workshop_number_unique");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

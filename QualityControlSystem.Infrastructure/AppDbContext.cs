using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.Infrastructure.Enums;

namespace QualityControlSystem.Infrastructure;

public class AppDbContext : DbContext
{
    // DbSets for all entities
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public DbSet<AccessRight> AccessRights { get; set; } = null!;
    public DbSet<Batch> Batches { get; set; } = null!;
    public DbSet<Camera> Cameras { get; set; } = null!;
    public DbSet<CameraFrame> CameraFrames { get; set; } = null!;
    public DbSet<EquipmentInspectionForm> EquipmentInspectionForms { get; set; } = null!;
    public DbSet<EquipmentInspectionFormParams> EquipmentInspectionFormParams { get; set; } = null!;
    public DbSet<EquipmentInspectionFormProductionEquipment> EquipmentInspectionFormProductionEquipments { get; set; } = null!;
    public DbSet<Frame> Frames { get; set; } = null!;
    public DbSet<FrameTestForm> FrameTestForms { get; set; } = null!;
    public DbSet<FrameTestFormFrame> FrameTestFormFrames { get; set; } = null!;
    public DbSet<FrameTestFormParams> FrameTestFormParams { get; set; } = null!;
    public DbSet<Instruction> Instructions { get; set; } = null!;
    public DbSet<InstructionProductionEquipment> InstructionProductionEquipments { get; set; } = null!;
    public DbSet<InstructionProductionOrder> InstructionProductionOrders { get; set; } = null!;
    public DbSet<Material> Materials { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Params> Params { get; set; } = null!;
    public DbSet<ProductionEquipment> ProductionEquipments { get; set; } = null!;
    public DbSet<ProductionOrder> ProductionOrders { get; set; } = null!;
    public DbSet<RegulatoryInformation> RegulatoryInformations { get; set; } = null!;
    public DbSet<RegulatoryInformationInstruction> RegulatoryInformationInstructions { get; set; } = null!;
    public DbSet<RegulatoryInformationProductionEquipment> RegulatoryInformationProductionEquipments { get; set; } = null!;
    public DbSet<RequisitionInvoice> RequisitionInvoices { get; set; } = null!;
    public DbSet<Sensor> Sensors { get; set; } = null!;
    public DbSet<SensorReadings> SensorReadings { get; set; } = null!;
    public DbSet<UserProfileAccessRights> UserProfileAccessRights { get; set; } = null!;
    public DbSet<Workshop> Workshops { get; set; } = null!;
    public DbSet<WorkshopProductionOrder> WorkshopProductionOrders { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        // Relationships and cascade behaviours
        modelBuilder.Entity<UserProfile>()
            .HasOne(up => up.Workshop)
            .WithMany()
            .HasForeignKey(up => up.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserProfileAccessRights>()
            .HasOne(ua => ua.UserProfile)
            .WithMany(up => up.AccessRights)
            .HasForeignKey(ua => ua.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProfileAccessRights>()
            .HasOne(ua => ua.AccessRight)
            .WithMany()
            .HasForeignKey(ua => ua.AccessRightId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.ProductionOrder)
            .WithMany()
            .HasForeignKey(b => b.ProductionOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Camera>()
            .HasOne(c => c.Workshop)
            .WithMany()
            .HasForeignKey(c => c.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CameraFrame>()
            .HasOne(cf => cf.Camera)
            .WithMany()
            .HasForeignKey(cf => cf.CameraId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CameraFrame>()
            .HasOne(cf => cf.Frame)
            .WithMany()
            .HasForeignKey(cf => cf.FrameId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EquipmentInspectionForm>()
            .HasOne(e => e.UserProfile)
            .WithMany()
            .HasForeignKey(e => e.UserProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EquipmentInspectionFormParams>()
            .HasOne(eip => eip.EquipmentInspectionForm)
            .WithMany()
            .HasForeignKey(eip => eip.EquipmentInspectionFormId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EquipmentInspectionFormParams>()
            .HasOne(eip => eip.Params)
            .WithMany()
            .HasForeignKey(eip => eip.ParamsId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EquipmentInspectionFormProductionEquipment>()
            .HasOne(epe => epe.EquipmentInspectionForm)
            .WithMany()
            .HasForeignKey(epe => epe.EquipmentInspectionFormId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EquipmentInspectionFormProductionEquipment>()
            .HasOne(epe => epe.ProductionEquipment)
            .WithMany()
            .HasForeignKey(epe => epe.ProductionEquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Frame>()
            .HasOne(f => f.Instruction)
            .WithMany()
            .HasForeignKey(f => f.InstructionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Frame>()
            .HasOne(f => f.Batch)
            .WithMany()
            .HasForeignKey(f => f.BatchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Frame>()
            .HasOne(f => f.Workshop)
            .WithMany()
            .HasForeignKey(f => f.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FrameTestForm>()
            .HasOne(ftf => ftf.UserProfile)
            .WithMany()
            .HasForeignKey(ftf => ftf.UserProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FrameTestFormFrame>()
            .HasOne(ftff => ftff.Frame)
            .WithMany()
            .HasForeignKey(ftff => ftff.FrameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FrameTestFormFrame>()
            .HasOne(ftff => ftff.FrameTestForm)
            .WithMany()
            .HasForeignKey(ftff => ftff.FrameTestFormId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FrameTestFormParams>()
            .HasOne(ftfp => ftfp.FrameTestForm)
            .WithMany()
            .HasForeignKey(ftfp => ftfp.FrameTestFormId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FrameTestFormParams>()
            .HasOne(ftfp => ftfp.Params)
            .WithMany()
            .HasForeignKey(ftfp => ftfp.ParamsId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InstructionProductionEquipment>()
            .HasOne(ipe => ipe.Instruction)
            .WithMany()
            .HasForeignKey(ipe => ipe.InstructionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InstructionProductionEquipment>()
            .HasOne(ipe => ipe.ProductionEquipment)
            .WithMany()
            .HasForeignKey(ipe => ipe.ProductionEquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InstructionProductionOrder>()
            .HasOne(i po => i po.Instruction)
            .WithMany()
            .HasForeignKey(i po => i po.InstructionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InstructionProductionOrder>()
            .HasOne(i po => i po.ProductionOrder)
            .WithMany()
            .HasForeignKey(i po => i po.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Frame)
            .WithMany()
            .HasForeignKey(n => n.FrameId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.ProductionEquipment)
            .WithMany()
            .HasForeignKey(n => n.ProductionEquipmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductionEquipment>()
            .HasOne(pe => pe.Workshop)
            .WithMany()
            .HasForeignKey(pe => pe.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RegulatoryInformationInstruction>()
            .HasOne(rii => rii.RegulatoryInformation)
            .WithMany()
            .HasForeignKey(rii => rii.RegulatoryInformationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RegulatoryInformationInstruction>()
            .HasOne(rii => rii.Instruction)
            .WithMany()
            .HasForeignKey(rii => rii.InstructionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RegulatoryInformationProductionEquipment>()
            .HasOne(rip => rip.RegulatoryInformation)
            .WithMany()
            .HasForeignKey(rip => rip.RegulatoryInformationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RegulatoryInformationProductionEquipment>()
            .HasOne(rip => rip.ProductionEquipment)
            .WithMany()
            .HasForeignKey(rip => rip.ProductionEquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RequisitionInvoice>()
            .HasOne(ri => ri.ProductionOrder)
            .WithMany()
            .HasForeignKey(ri => ri.ProductionOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Sensor>()
            .HasOne(s => s.Workshop)
            .WithMany()
            .HasForeignKey(s => s.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SensorReadings>()
            .HasOne(sr => sr.Sensor)
            .WithMany()
            .HasForeignKey(sr => sr.SensorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SensorReadings>()
            .HasOne(sr => sr.Frame)
            .WithMany()
            .HasForeignKey(sr => sr.FrameId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SensorReadings>()
            .HasOne(sr => sr.ProductionEquipment)
            .WithMany()
            .HasForeignKey(sr => sr.ProductionEquipmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WorkshopProductionOrder>()
            .HasOne(wpo => wpo.Workshop)
            .WithMany()
            .HasForeignKey(wpo => wpo.WorkshopId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkshopProductionOrder>()
            .HasOne(wpo => wpo.ProductionOrder)
            .WithMany()
            .HasForeignKey(wpo => wpo.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes (example: unique index on UserProfile.PersonnelNumber)
        modelBuilder.Entity<UserProfile>()
            .HasIndex(up => up.PersonnelNumber)
            .IsUnique();

        modelBuilder.Entity<Workshop>()
            .HasIndex(w => w.Number)
            .IsUnique();

        // Seed data (kept from original file)
        modelBuilder.Entity<UserProfile>().HasData(
            new UserProfile
            {
                UserProfileId = 1,
                Name = "Админ",
                Surname = "Админов",
                Patron = null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                PersonnelNumber = "A000001",
                Role = UserRole.Admin,
                WorkshopId = 0
            },
            new UserProfile
            {
                UserProfileId = 2,
                Name = "Иван",
                Surname = "Петров",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("operator"),
                PersonnelNumber = "O123456",
                Role = UserRole.Operator,
                WorkshopId = 1
            },
            new UserProfile
            {
                UserProfileId = 3,
                Name = "Сергей",
                Surname = "Сидоров",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("equipment"),
                PersonnelNumber = "E789012",
                Role = UserRole.EquipmentSpecialist,
                WorkshopId = 1
            },
            new UserProfile
            {
                UserProfileId = 4,
                Name = "Мария",
                Surname = "Иванова",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("qcofficer"),
                PersonnelNumber = "Q345678",
                Role = UserRole.QualityControlOfficer,
                WorkshopId = 1
            }
        );
    }
}

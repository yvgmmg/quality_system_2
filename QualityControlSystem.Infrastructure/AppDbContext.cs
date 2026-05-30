using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Entities;

namespace QualityControlSystem.Infrastructure;

public partial class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessRight> AccessRights { get; set; }

    public virtual DbSet<Batch> Batches { get; set; }

    public virtual DbSet<Camera> Cameras { get; set; }

    public virtual DbSet<CameraFrame> CameraFrames { get; set; }

    public virtual DbSet<EquipmentInspectionForm> EquipmentInspectionForms { get; set; }

    public virtual DbSet<EquipmentInspectionFormParam> EquipmentInspectionFormParams { get; set; }

    public virtual DbSet<EquipmentInspectionFormProductionEquipment> EquipmentInspectionFormProductionEquipments { get; set; }

    public virtual DbSet<Frame> Frames { get; set; }

    public virtual DbSet<FrameTestForm> FrameTestForms { get; set; }

    public virtual DbSet<FrameTestFormFrame> FrameTestFormFrames { get; set; }

    public virtual DbSet<FrameTestFormParam> FrameTestFormParams { get; set; }

    public virtual DbSet<Instruction> Instructions { get; set; }

    public virtual DbSet<InstructionProductionEquipment> InstructionProductionEquipments { get; set; }

    public virtual DbSet<InstructionProductionOrder> InstructionProductionOrders { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Param> Params { get; set; }

    public virtual DbSet<ProductionEquipment> ProductionEquipments { get; set; }

    public virtual DbSet<ProductionOrder> ProductionOrders { get; set; }

    public virtual DbSet<RegilatoryInformationInstruction> RegilatoryInformationInstructions { get; set; }

    public virtual DbSet<RegulatoryInformation> RegulatoryInformations { get; set; }

    public virtual DbSet<RegulatoryInfromationProductionEquipment> RegulatoryInfromationProductionEquipments { get; set; }

    public virtual DbSet<RequisitionInvoice> RequisitionInvoices { get; set; }

    public virtual DbSet<Sensor> Sensors { get; set; }

    public virtual DbSet<SensorReading> SensorReadings { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserProfileAccessRight> UserProfileAccessRights { get; set; }

    public virtual DbSet<Workshop> Workshops { get; set; }

    public virtual DbSet<WorkshopProductionOrder> WorkshopProductionOrders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Database=quality_system;Username=postgres;Password=1234");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("frame_result", new[] { "ok", "for_rework", "defective" })
            .HasPostgresEnum("inspection_result", new[] { "in progress", "ok", "normal", "repair" })
            .HasPostgresEnum("measurement_unit", new[] { "мм", "см", "м", "г", "кг", "т", "°C", "°", "%", "шт", "Н", "МПа", "В", "А", "м/с", "м²", "м³", "безразм." })
            .HasPostgresEnum("role", new[] { "admin", "operator", "equipment specialist", "quality control officer" })
            .HasPostgresEnum("severity", new[] { "warning", "critical" })
            .HasPostgresEnum("source", new[] { "equipment", "frame" })
            .HasPostgresEnum("test_result", new[] { "ok", "defective" });

        modelBuilder.Entity<AccessRight>(entity =>
        {
            entity.HasKey(e => e.AccessRightId).HasName("access_rights_pk");
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.HasKey(e => e.BatchId).HasName("batch_pk");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.Batches)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("batch_production_order_fk");
        });

        modelBuilder.Entity<Camera>(entity =>
        {
            entity.HasKey(e => e.CameraId).HasName("camera_pk");

            entity.HasOne(d => d.Workshop).WithMany(p => p.Cameras).HasConstraintName("camera_workshop_fk");
        });

        modelBuilder.Entity<CameraFrame>(entity =>
        {
            entity.HasKey(e => e.CameraFrameId).HasName("camera_frame_pk");

            entity.HasOne(d => d.Camera).WithMany(p => p.CameraFrames).HasConstraintName("camera_frame_camera_fk");

            entity.HasOne(d => d.Frame).WithMany(p => p.CameraFrames).HasConstraintName("camera_frame_frame_fk");
        });

        modelBuilder.Entity<EquipmentInspectionForm>(entity =>
        {
            entity.HasKey(e => e.EquipmentInspectionFormId).HasName("equipment_inspection_form_pk");

            entity.HasOne(d => d.UserProfile).WithMany(p => p.EquipmentInspectionForms).HasConstraintName("equipment_inspection_form_user_profile_fk");
        });

        modelBuilder.Entity<EquipmentInspectionFormParam>(entity =>
        {
            entity.HasKey(e => e.EquipmentInspectionFormParamsId).HasName("equipment_inspection_form_params_pk");

            entity.Property(e => e.EquipmentInspectionFormParamsId).HasDefaultValueSql("nextval('equipment_inspection_form_par_equipment_inspection_form_par_seq'::regclass)");

            entity.HasOne(d => d.EquipmentInspectionForm).WithMany(p => p.EquipmentInspectionFormParams).HasConstraintName("equipment_inspection_form_params_equipment_inspection_form_fk");

            entity.HasOne(d => d.Params).WithMany(p => p.EquipmentInspectionFormParams).HasConstraintName("equipment_inspection_form_params_params_fk");
        });

        modelBuilder.Entity<EquipmentInspectionFormProductionEquipment>(entity =>
        {
            entity.HasKey(e => e.EquipmentInspectionFormProductionEquipment1).HasName("equipment_inspection_form_production_equipment_pk");

            entity.Property(e => e.EquipmentInspectionFormProductionEquipment1).HasDefaultValueSql("nextval('equipment_inspection_form_pro_equipment_inspection_form_pro_seq'::regclass)");

            entity.HasOne(d => d.EquipmentInspectionForm).WithMany(p => p.EquipmentInspectionFormProductionEquipments).HasConstraintName("equipment_inspection_form_production_equipment_equipment_inspec");

            entity.HasOne(d => d.ProductionEquipment).WithMany(p => p.EquipmentInspectionFormProductionEquipments).HasConstraintName("equipment_inspection_form_production_equipment_production_equip");
        });

        modelBuilder.Entity<Frame>(entity =>
        {
            entity.HasKey(e => e.FrameId).HasName("frame_pk");

            entity.HasOne(d => d.Batch).WithMany(p => p.Frames).HasConstraintName("frame_batch_fk");

            entity.HasOne(d => d.Instruction).WithMany(p => p.Frames).HasConstraintName("frame_instruction_fk");

            entity.HasOne(d => d.Workshop).WithMany(p => p.Frames).HasConstraintName("frame_workshop_fk");
        });

        modelBuilder.Entity<FrameTestForm>(entity =>
        {
            entity.HasKey(e => e.FrameTestFormId).HasName("frame_test_form_pk");

            entity.HasOne(d => d.UserProfile).WithMany(p => p.FrameTestForms).HasConstraintName("frame_test_form_user_profile_fk");
        });

        modelBuilder.Entity<FrameTestFormFrame>(entity =>
        {
            entity.HasKey(e => e.FrameTestFormFrameId).HasName("frame_test_form_frame_pk");

            entity.HasOne(d => d.Frame).WithMany(p => p.FrameTestFormFrames).HasConstraintName("frame_test_form_frame_frame_fk");

            entity.HasOne(d => d.FrameTestForm).WithMany(p => p.FrameTestFormFrames).HasConstraintName("frame_test_form_frame_frame_test_form_fk");
        });

        modelBuilder.Entity<FrameTestFormParam>(entity =>
        {
            entity.HasKey(e => e.FrameTestFormParamsId).HasName("frame_test_form_params_pk");

            entity.HasOne(d => d.FrameTestForm).WithMany(p => p.FrameTestFormParams).HasConstraintName("frame_test_form_params_frame_test_form_fk");

            entity.HasOne(d => d.Params).WithMany(p => p.FrameTestFormParams).HasConstraintName("frame_test_form_params_params_fk");
        });

        modelBuilder.Entity<Instruction>(entity =>
        {
            entity.HasKey(e => e.InstructionId).HasName("instruction_pk");
        });

        modelBuilder.Entity<InstructionProductionEquipment>(entity =>
        {
            entity.HasKey(e => e.InstructionProductionEquipmentId).HasName("instruction_production_equipment_pk");

            entity.Property(e => e.InstructionProductionEquipmentId).HasDefaultValueSql("nextval('instruction_production_equipment_instruction_production_equipme'::regclass)");

            entity.HasOne(d => d.Instruction).WithMany(p => p.InstructionProductionEquipments)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("instruction_production_equipment_instruction_fk");

            entity.HasOne(d => d.ProductionEquipment).WithMany(p => p.InstructionProductionEquipments)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("instruction_production_equipment_production_equipment_fk");
        });

        modelBuilder.Entity<InstructionProductionOrder>(entity =>
        {
            entity.HasKey(e => e.InstructionProductionOrderId).HasName("instruction_production_order_pk");

            entity.Property(e => e.InstructionProductionOrderId).HasDefaultValueSql("nextval('instruction_production_order_instruction_production_order_i_seq'::regclass)");

            entity.HasOne(d => d.Instruction).WithMany(p => p.InstructionProductionOrders).HasConstraintName("instruction_production_order_instruction_fk");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.InstructionProductionOrders).HasConstraintName("instruction_production_order_production_order_fk");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("material_pk");

            entity.HasOne(d => d.RequisitionInvoice).WithMany(p => p.Materials).HasConstraintName("material_requisition_invoice_fk");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notification_pk");

            entity.HasOne(d => d.Frame).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notification_frame_fk");

            entity.HasOne(d => d.ProductionEquipment).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notification_production_equipment_fk");

            entity.HasOne(d => d.UserProfile).WithMany(p => p.Notifications).HasConstraintName("notification_user_profile_fk");
        });

        modelBuilder.Entity<Param>(entity =>
        {
            entity.HasKey(e => e.ParamsId).HasName("params_pk");
        });

        modelBuilder.Entity<ProductionEquipment>(entity =>
        {
            entity.HasKey(e => e.ProductionEquipmentId).HasName("production_equipment_pk");

            entity.HasOne(d => d.Workshop).WithMany(p => p.ProductionEquipments).HasConstraintName("production_equipment_workshop_fk");
        });

        modelBuilder.Entity<ProductionOrder>(entity =>
        {
            entity.HasKey(e => e.ProductionOrderId).HasName("production_order_pk");
        });

        modelBuilder.Entity<RegilatoryInformationInstruction>(entity =>
        {
            entity.HasKey(e => e.RegulatoryInformationInstructionId).HasName("regilatory_information_instruction_pk");

            entity.Property(e => e.RegulatoryInformationInstructionId).HasDefaultValueSql("nextval('regilatory_information_instru_regilatory_information_instru_seq'::regclass)");

            entity.HasOne(d => d.Instruction).WithMany(p => p.RegilatoryInformationInstructions).HasConstraintName("regilatory_information_instruction_instruction_fk");

            entity.HasOne(d => d.RegulatoryInformation).WithMany(p => p.RegilatoryInformationInstructions).HasConstraintName("regilatory_information_instruction_regulatory_information_fk");
        });

        modelBuilder.Entity<RegulatoryInformation>(entity =>
        {
            entity.HasKey(e => e.RegulatoryInformationId).HasName("regulatory_information_pk");

            entity.Property(e => e.MaxValue).HasComment("Максимально допустимое значение (не более)");
            entity.Property(e => e.MinValue).HasComment("Минимально допустимое значение (не менее)");
        });

        modelBuilder.Entity<RegulatoryInfromationProductionEquipment>(entity =>
        {
            entity.HasKey(e => e.RegulatoryInformationProductionEquipmentId).HasName("regulatory_infromation_production_equipment_pk");

            entity.Property(e => e.RegulatoryInformationProductionEquipmentId).HasDefaultValueSql("nextval('regulatory_infromation_produc_regulatory_infromation_produc_seq'::regclass)");

            entity.HasOne(d => d.ProductionEquipment).WithMany(p => p.RegulatoryInfromationProductionEquipments).HasConstraintName("regulatory_infromation_production_equipment_production_equipmen");

            entity.HasOne(d => d.RegulatoryInformation).WithMany(p => p.RegulatoryInfromationProductionEquipments).HasConstraintName("regulatory_infromation_production_equipment_regulatory_informat");
        });

        modelBuilder.Entity<RequisitionInvoice>(entity =>
        {
            entity.HasKey(e => e.RequisitionInvoiceId).HasName("requisition_invoice_pk");

            entity.Property(e => e.RequisitionInvoiceId).HasDefaultValueSql("nextval('frame_test_form_params_frame_test_form_params_id_seq'::regclass)");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.RequisitionInvoices)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("requisition_invoice_production_order_fk");
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.SensorId).HasName("sensor_pk");

            entity.HasOne(d => d.Workshop).WithMany(p => p.Sensors)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sensor_workshop_fk");
        });

        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(e => e.SensorReadingsId).HasName("sensor_readings_pk");

            entity.HasOne(d => d.Frame).WithMany(p => p.SensorReadings).HasConstraintName("sensor_readings_frame_fk");

            entity.HasOne(d => d.ProductionEquipment).WithMany(p => p.SensorReadings)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("sensor_readings_production_equipment_fk");

            entity.HasOne(d => d.Sensor).WithMany(p => p.SensorReadings).HasConstraintName("sensor_readings_sensor_fk");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserProfileId).HasName("user_profile_pk");

            entity.Property(e => e.Role).HasConversion<string>();

            entity.HasOne(d => d.Workshop).WithMany(p => p.UserProfiles).HasConstraintName("user_profile_workshop_fk");
        });

        modelBuilder.Entity<UserProfileAccessRight>(entity =>
        {
            entity.HasKey(e => e.UserProfileAccessRightsId).HasName("user_profile_access_rights_pk");

            entity.HasOne(d => d.AccessRight).WithMany(p => p.UserProfileAccessRights)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_profile_access_rights_access_rights_fk");

            entity.HasOne(d => d.UserProfile).WithMany(p => p.UserProfileAccessRights).HasConstraintName("user_profile_access_rights_user_profile_fk");
        });

        modelBuilder.Entity<Workshop>(entity =>
        {
            entity.HasKey(e => e.WorkshopId).HasName("workshop_pk");
        });

        modelBuilder.Entity<WorkshopProductionOrder>(entity =>
        {
            entity.HasKey(e => e.WorkshopProductionOrder1).HasName("workshop_production_order_pk");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.WorkshopProductionOrders)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("workshop_production_order_production_order_fk");

            entity.HasOne(d => d.Workshop).WithMany(p => p.WorkshopProductionOrders)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("workshop_production_order_workshop_fk");
        });
        modelBuilder.HasSequence("instruction_production_equipment_instruction_production_equipme").HasMax(2147483647L);
        modelBuilder.HasSequence("requisition_invoice_requisition_invoice_id_seq").HasMax(2147483647L);
        modelBuilder.HasSequence("workshop_production_order_workshop_production_order_seq").HasMax(2147483647L);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

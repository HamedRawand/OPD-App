using Microsoft.EntityFrameworkCore;
using OPDClinic.Models;

namespace OPDClinic.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>                   Users             => Set<User>();
    public DbSet<CustomRole>             CustomRoles       => Set<CustomRole>();
    public DbSet<Physician>              Physicians        => Set<Physician>();
    public DbSet<Patient>                Patients          => Set<Patient>();
    public DbSet<Visit>                  Visits            => Set<Visit>();
    public DbSet<MedicineUsage>          MedicineUsages    => Set<MedicineUsage>();
    public DbSet<MedicineList>           MedicineLists     => Set<MedicineList>();
    public DbSet<LabTest>                LabTests          => Set<LabTest>();
    public DbSet<Dosage>                 Dosages           => Set<Dosage>();
    public DbSet<MedicineForm>           MedicineForms     => Set<MedicineForm>();
    public DbSet<RouteOfAdministration>  Routes            => Set<RouteOfAdministration>();
    public DbSet<MedicineNote>           MedicineNotes     => Set<MedicineNote>();
    public DbSet<PrescriptionNote>       PrescriptionNotes => Set<PrescriptionNote>();
    public DbSet<PatientLabTest>         PatientLabTests   => Set<PatientLabTest>();
    public DbSet<AuditLog>               AuditLogs         => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Physician)
            .WithMany()
            .HasForeignKey(u => u.PhysicianId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasOne(u => u.CustomRole)
            .WithMany()
            .HasForeignKey(u => u.CustomRoleId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Patient ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.PatientCode)
            .IsUnique()
            .HasFilter("\"PatientCode\" IS NOT NULL");

        // ── Visit ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Visit>()
            .HasOne(v => v.Patient)
            .WithMany(p => p.Visits)
            .HasForeignKey(v => v.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Visit>()
            .HasOne(v => v.Physician)
            .WithMany(ph => ph.Visits)
            .HasForeignKey(v => v.PhysicianId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── MedicineUsage ─────────────────────────────────────────────────────
        modelBuilder.Entity<MedicineUsage>()
            .HasOne(m => m.Visit)
            .WithMany(v => v.Medicines)
            .HasForeignKey(m => m.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── PatientLabTest ────────────────────────────────────────────────────
        modelBuilder.Entity<PatientLabTest>()
            .HasOne(pl => pl.Visit)
            .WithMany(v => v.LabTests)
            .HasForeignKey(pl => pl.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientLabTest>()
            .HasOne(pl => pl.LabTest)
            .WithMany()
            .HasForeignKey(pl => pl.LabTestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using OPDClinic.Models;

namespace OPDClinic.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Physician> Physicians => Set<Physician>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicineUsage> MedicineUsages => Set<MedicineUsage>();
    public DbSet<MedicineList> MedicineLists => Set<MedicineList>();
    public DbSet<LabTest> LabTests => Set<LabTest>();
    public DbSet<Dosage> Dosages => Set<Dosage>();
    public DbSet<MedicineForm> MedicineForms => Set<MedicineForm>();
    public DbSet<RouteOfAdministration> Routes => Set<RouteOfAdministration>();
    public DbSet<MedicineNote> MedicineNotes => Set<MedicineNote>();
    public DbSet<PrescriptionNote> PrescriptionNotes => Set<PrescriptionNote>();
    public DbSet<PatientLabTest> PatientLabTests => Set<PatientLabTest>();
    public DbSet<AuditLog>       AuditLogs        => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Patient>()
            .HasOne(p => p.Physician)
            .WithMany(ph => ph.Patients)
            .HasForeignKey(p => p.PhysicianId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MedicineUsage>()
            .HasOne(m => m.Patient)
            .WithMany(p => p.Medicines)
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientLabTest>()
            .HasOne(pl => pl.Patient)
            .WithMany()
            .HasForeignKey(pl => pl.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientLabTest>()
            .HasOne(pl => pl.LabTest)
            .WithMany()
            .HasForeignKey(pl => pl.LabTestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

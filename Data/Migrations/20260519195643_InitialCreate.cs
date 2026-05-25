using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPDClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dosages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DosageText = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dosages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabTests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    TestName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Abbreviation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Specimen = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicineForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    FormName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Abbreviation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineForms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicineLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MedicineName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    GenericName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Strength = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicineNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Physicians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FKey = table.Column<int>(type: "INTEGER", nullable: false),
                    NameEng = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    NameDari = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SpecialityEng = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SpecialityDari = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    OtherSpecialityEng = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    OtherSpecialityDari = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: true),
                    ContactNumber = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    WhatsAppNumber = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ReceptionContactNumber = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Physicians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RouteName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Abbreviation = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhysicianId = table.Column<int>(type: "INTEGER", nullable: true),
                    OpdDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HijriDate = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PatientName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Age = table.Column<int>(type: "INTEGER", nullable: true),
                    Sex = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PatientNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BP = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    HR = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PR = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RR = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BT = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BW = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ClinicalFindings = table.Column<string>(type: "TEXT", nullable: true),
                    Diagnosis = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Physicians_PhysicianId",
                        column: x => x.PhysicianId,
                        principalTable: "Physicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MedicineUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Prescription = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Strength = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Qty = table.Column<int>(type: "INTEGER", nullable: true),
                    Usage = table.Column<string>(type: "TEXT", nullable: true),
                    RouteName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineUsages_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineUsages_PatientId",
                table: "MedicineUsages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PhysicianId",
                table: "Patients",
                column: "PhysicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dosages");

            migrationBuilder.DropTable(
                name: "LabTests");

            migrationBuilder.DropTable(
                name: "MedicineForms");

            migrationBuilder.DropTable(
                name: "MedicineLists");

            migrationBuilder.DropTable(
                name: "MedicineNotes");

            migrationBuilder.DropTable(
                name: "MedicineUsages");

            migrationBuilder.DropTable(
                name: "PrescriptionNotes");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Physicians");
        }
    }
}

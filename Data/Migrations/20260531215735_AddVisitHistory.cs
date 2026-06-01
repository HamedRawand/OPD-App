using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPDClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Drop FKs that we're about to replace ───────────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_MedicineUsages_Patients_PatientId",
                table: "MedicineUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientLabTests_Patients_PatientId",
                table: "PatientLabTests");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Physicians_PhysicianId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_PhysicianId",
                table: "Patients");

            // ── 2. Create Visits table FIRST — while Patients still has clinical columns ──
            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId        = table.Column<int>(type: "INTEGER", nullable: false),
                    PhysicianId      = table.Column<int>(type: "INTEGER", nullable: true),
                    OpdDate          = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HijriDate        = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Age              = table.Column<int>(type: "INTEGER", nullable: true),
                    BP               = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    HR               = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PR               = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RR               = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BT               = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BW               = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ClinicalFindings = table.Column<string>(type: "TEXT", nullable: true),
                    Diagnosis        = table.Column<string>(type: "TEXT", nullable: true),
                    FooterNote       = table.Column<string>(type: "TEXT", nullable: true),
                    Note             = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated      = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visits_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Visits_Physicians_PhysicianId",
                        column: x => x.PhysicianId,
                        principalTable: "Physicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // ── 3. Data migration: one Visit per existing Patient ─────────────
            migrationBuilder.Sql(@"
                INSERT INTO ""Visits"" (
                    ""PatientId"", ""PhysicianId"", ""OpdDate"", ""HijriDate"", ""Age"",
                    ""BP"", ""HR"", ""PR"", ""RR"", ""BT"", ""BW"",
                    ""ClinicalFindings"", ""Diagnosis"", ""FooterNote"", ""Note"", ""LastUpdated"")
                SELECT
                    ""Id"", ""PhysicianId"", ""OpdDate"", ""HijriDate"", ""Age"",
                    ""BP"", ""HR"", ""PR"", ""RR"", ""BT"", ""BW"",
                    ""ClinicalFindings"", ""Diagnosis"", ""FooterNote"", ""Note"", ""LastUpdated""
                FROM ""Patients"";
            ");

            // ── 4. Rename PatientId → VisitId in MedicineUsages / PatientLabTests ──
            //       (values still hold old Patient IDs — will be corrected below)
            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "PatientLabTests",
                newName: "VisitId");

            migrationBuilder.RenameIndex(
                name: "IX_PatientLabTests_PatientId",
                table: "PatientLabTests",
                newName: "IX_PatientLabTests_VisitId");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "MedicineUsages",
                newName: "VisitId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicineUsages_PatientId",
                table: "MedicineUsages",
                newName: "IX_MedicineUsages_VisitId");

            // ── 5. Fix VisitId values — map old PatientId → actual Visit.Id ───
            //       Each patient has exactly one Visit, so the subquery is unambiguous.
            migrationBuilder.Sql(@"
                UPDATE ""MedicineUsages""
                SET ""VisitId"" = (
                    SELECT ""Id"" FROM ""Visits""
                    WHERE ""PatientId"" = ""MedicineUsages"".""VisitId""
                    LIMIT 1
                );
            ");

            migrationBuilder.Sql(@"
                UPDATE ""PatientLabTests""
                SET ""VisitId"" = (
                    SELECT ""Id"" FROM ""Visits""
                    WHERE ""PatientId"" = ""PatientLabTests"".""VisitId""
                    LIMIT 1
                );
            ");

            // ── 6. Rebuild Patients table: drop clinical columns, rename OpdDate→CreatedAt ──
            migrationBuilder.DropColumn(name: "Age",              table: "Patients");
            migrationBuilder.DropColumn(name: "BP",               table: "Patients");
            migrationBuilder.DropColumn(name: "BT",               table: "Patients");
            migrationBuilder.DropColumn(name: "BW",               table: "Patients");
            migrationBuilder.DropColumn(name: "ClinicalFindings", table: "Patients");
            migrationBuilder.DropColumn(name: "Diagnosis",        table: "Patients");
            migrationBuilder.DropColumn(name: "FooterNote",       table: "Patients");
            migrationBuilder.DropColumn(name: "HR",               table: "Patients");
            migrationBuilder.DropColumn(name: "HijriDate",        table: "Patients");
            migrationBuilder.DropColumn(name: "LastUpdated",      table: "Patients");
            migrationBuilder.DropColumn(name: "Note",             table: "Patients");
            migrationBuilder.DropColumn(name: "PR",               table: "Patients");
            migrationBuilder.DropColumn(name: "PhysicianId",      table: "Patients");
            migrationBuilder.DropColumn(name: "RR",               table: "Patients");

            // OpdDate → CreatedAt (existing value is the last-visit date — acceptable for legacy rows)
            migrationBuilder.RenameColumn(
                name: "OpdDate",
                table: "Patients",
                newName: "CreatedAt");

            // ── 7. Create indexes on Visits ───────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Visits_PatientId",
                table: "Visits",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PhysicianId",
                table: "Visits",
                column: "PhysicianId");

            // ── 8. Re-add FK constraints pointing to Visits ───────────────────
            migrationBuilder.AddForeignKey(
                name: "FK_MedicineUsages_Visits_VisitId",
                table: "MedicineUsages",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientLabTests_Visits_VisitId",
                table: "PatientLabTests",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicineUsages_Visits_VisitId",
                table: "MedicineUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientLabTests_Visits_VisitId",
                table: "PatientLabTests");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Patients",
                newName: "OpdDate");

            migrationBuilder.RenameColumn(
                name: "VisitId",
                table: "PatientLabTests",
                newName: "PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_PatientLabTests_VisitId",
                table: "PatientLabTests",
                newName: "IX_PatientLabTests_PatientId");

            migrationBuilder.RenameColumn(
                name: "VisitId",
                table: "MedicineUsages",
                newName: "PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicineUsages_VisitId",
                table: "MedicineUsages",
                newName: "IX_MedicineUsages_PatientId");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Patients",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BP",
                table: "Patients",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BT",
                table: "Patients",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BW",
                table: "Patients",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClinicalFindings",
                table: "Patients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                table: "Patients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterNote",
                table: "Patients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HR",
                table: "Patients",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HijriDate",
                table: "Patients",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Patients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Patients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PR",
                table: "Patients",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhysicianId",
                table: "Patients",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RR",
                table: "Patients",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PhysicianId",
                table: "Patients",
                column: "PhysicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineUsages_Patients_PatientId",
                table: "MedicineUsages",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientLabTests_Patients_PatientId",
                table: "PatientLabTests",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Physicians_PhysicianId",
                table: "Patients",
                column: "PhysicianId",
                principalTable: "Physicians",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPDClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrescriptionTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    DefaultDiagnosis = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultClinicalFindings = table.Column<string>(type: "TEXT", nullable: true),
                    FooterNote = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionTemplateLabTests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrescriptionTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    LabTestId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionTemplateLabTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionTemplateLabTests_LabTests_LabTestId",
                        column: x => x.LabTestId,
                        principalTable: "LabTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrescriptionTemplateLabTests_PrescriptionTemplates_PrescriptionTemplateId",
                        column: x => x.PrescriptionTemplateId,
                        principalTable: "PrescriptionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionTemplateLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrescriptionTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Prescription = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Strength = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Qty = table.Column<int>(type: "INTEGER", nullable: true),
                    Usage = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionTemplateLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionTemplateLines_PrescriptionTemplates_PrescriptionTemplateId",
                        column: x => x.PrescriptionTemplateId,
                        principalTable: "PrescriptionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplateLabTests_LabTestId",
                table: "PrescriptionTemplateLabTests",
                column: "LabTestId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplateLabTests_PrescriptionTemplateId",
                table: "PrescriptionTemplateLabTests",
                column: "PrescriptionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplateLines_PrescriptionTemplateId",
                table: "PrescriptionTemplateLines",
                column: "PrescriptionTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrescriptionTemplateLabTests");

            migrationBuilder.DropTable(
                name: "PrescriptionTemplateLines");

            migrationBuilder.DropTable(
                name: "PrescriptionTemplates");
        }
    }
}

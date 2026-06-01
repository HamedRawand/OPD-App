using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPDClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineStrengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicineStrengths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MedicineListId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineStrengths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineStrengths_MedicineLists_MedicineListId",
                        column: x => x.MedicineListId,
                        principalTable: "MedicineLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineStrengths_MedicineListId",
                table: "MedicineStrengths",
                column: "MedicineListId");

            // ── Data migration: copy legacy Strength field into MedicineStrengths ──
            migrationBuilder.Sql(@"
                INSERT INTO MedicineStrengths (MedicineListId, Value)
                SELECT Id, TRIM(Strength)
                FROM MedicineLists
                WHERE Strength IS NOT NULL AND TRIM(Strength) != ''
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicineStrengths");
        }
    }
}

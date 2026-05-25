using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPDClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineNoteCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MedicineNotes",
                type: "TEXT",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "MedicineNotes");
        }
    }
}

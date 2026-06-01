using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPDClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPhysicianLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhysicianId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhysicianId",
                table: "Users",
                column: "PhysicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Physicians_PhysicianId",
                table: "Users",
                column: "PhysicianId",
                principalTable: "Physicians",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Physicians_PhysicianId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PhysicianId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhysicianId",
                table: "Users");
        }
    }
}

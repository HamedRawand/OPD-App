using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPDClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicInfoAndNextVisitDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NextVisitDate",
                table: "Visits",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClinicNameDari",
                table: "Physicians",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClinicNameEng",
                table: "Physicians",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "Physicians",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextVisitDate",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ClinicNameDari",
                table: "Physicians");

            migrationBuilder.DropColumn(
                name: "ClinicNameEng",
                table: "Physicians");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "Physicians");
        }
    }
}

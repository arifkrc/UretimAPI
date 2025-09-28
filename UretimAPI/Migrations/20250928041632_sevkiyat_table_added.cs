using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UretimAPI.Migrations
{
    /// <inheritdoc />
    public partial class sevkiyat_table_added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Disk = table.Column<int>(type: "int", nullable: true),
                    Kampana = table.Column<int>(type: "int", nullable: true),
                    Poyra = table.Column<int>(type: "int", nullable: true),
                    Abroad = table.Column<bool>(type: "bit", nullable: false),
                    Domestic = table.Column<bool>(type: "bit", nullable: false),
                    AddedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_Date_IsActive",
                table: "Shipments",
                columns: new[] { "Date", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shipments");
        }
    }
}

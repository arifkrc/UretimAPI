using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UretimAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleTimeUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CycleTime_Product_Operation_IsActive",
                table: "CycleTimes");

            migrationBuilder.CreateIndex(
                name: "IX_CycleTime_ProductId_OperationId_Unique",
                table: "CycleTimes",
                columns: new[] { "ProductId", "OperationId" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CycleTime_ProductId_OperationId_Unique",
                table: "CycleTimes");

            migrationBuilder.CreateIndex(
                name: "IX_CycleTime_Product_Operation_IsActive",
                table: "CycleTimes",
                columns: new[] { "ProductId", "OperationId", "IsActive" });
        }
    }
}

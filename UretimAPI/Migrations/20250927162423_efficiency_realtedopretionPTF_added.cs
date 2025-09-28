using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UretimAPI.Migrations
{
    /// <inheritdoc />
    public partial class efficiency_realtedopretionPTF_added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PTF_Operation_Date_IsActive",
                table: "ProductionTrackingForms");

            migrationBuilder.DropColumn(
                name: "Operation",
                table: "ProductionTrackingForms");

            migrationBuilder.AddColumn<double>(
                name: "MachineEfficiency",
                table: "ProductionTrackingForms",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperationId",
                table: "ProductionTrackingForms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "OperatorEfficiency",
                table: "ProductionTrackingForms",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PTF_OperationId_Date_IsActive",
                table: "ProductionTrackingForms",
                columns: new[] { "OperationId", "Date", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionTrackingForms_Operations_OperationId",
                table: "ProductionTrackingForms",
                column: "OperationId",
                principalTable: "Operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionTrackingForms_Operations_OperationId",
                table: "ProductionTrackingForms");

            migrationBuilder.DropIndex(
                name: "IX_PTF_OperationId_Date_IsActive",
                table: "ProductionTrackingForms");

            migrationBuilder.DropColumn(
                name: "MachineEfficiency",
                table: "ProductionTrackingForms");

            migrationBuilder.DropColumn(
                name: "OperationId",
                table: "ProductionTrackingForms");

            migrationBuilder.DropColumn(
                name: "OperatorEfficiency",
                table: "ProductionTrackingForms");

            migrationBuilder.AddColumn<string>(
                name: "Operation",
                table: "ProductionTrackingForms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PTF_Operation_Date_IsActive",
                table: "ProductionTrackingForms",
                columns: new[] { "Operation", "Date", "IsActive" });
        }
    }
}

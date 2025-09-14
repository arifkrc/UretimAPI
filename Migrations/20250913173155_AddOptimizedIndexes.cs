using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UretimAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionTrackingForms_ProductCode",
                table: "ProductionTrackingForms");

            migrationBuilder.DropIndex(
                name: "IX_Packings_ProductCode",
                table: "Packings");

            migrationBuilder.DropIndex(
                name: "IX_CycleTimes_ProductId_OperationId",
                table: "CycleTimes");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Type_IsActive",
                table: "Products",
                columns: new[] { "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PTF_Date_IsActive",
                table: "ProductionTrackingForms",
                columns: new[] { "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PTF_Operation_Date_IsActive",
                table: "ProductionTrackingForms",
                columns: new[] { "Operation", "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PTF_ProductCode_Date_IsActive",
                table: "ProductionTrackingForms",
                columns: new[] { "ProductCode", "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PTF_Shift_Date_IsActive",
                table: "ProductionTrackingForms",
                columns: new[] { "Shift", "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Packing_Date_IsActive",
                table: "Packings",
                columns: new[] { "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Packing_ProductCode_Date_IsActive",
                table: "Packings",
                columns: new[] { "ProductCode", "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Customer_IsActive",
                table: "Orders",
                columns: new[] { "Customer", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Week_IsActive",
                table: "Orders",
                columns: new[] { "OrderAddedDateTime", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CycleTime_Product_Operation_IsActive",
                table: "CycleTimes",
                columns: new[] { "ProductId", "OperationId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Type_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PTF_Date_IsActive",
                table: "ProductionTrackingForms");

            migrationBuilder.DropIndex(
                name: "IX_PTF_Operation_Date_IsActive",
                table: "ProductionTrackingForms");

            migrationBuilder.DropIndex(
                name: "IX_PTF_ProductCode_Date_IsActive",
                table: "ProductionTrackingForms");

            migrationBuilder.DropIndex(
                name: "IX_PTF_Shift_Date_IsActive",
                table: "ProductionTrackingForms");

            migrationBuilder.DropIndex(
                name: "IX_Packing_Date_IsActive",
                table: "Packings");

            migrationBuilder.DropIndex(
                name: "IX_Packing_ProductCode_Date_IsActive",
                table: "Packings");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Customer_IsActive",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Week_IsActive",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_CycleTime_Product_Operation_IsActive",
                table: "CycleTimes");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionTrackingForms_ProductCode",
                table: "ProductionTrackingForms",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_Packings_ProductCode",
                table: "Packings",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_CycleTimes_ProductId_OperationId",
                table: "CycleTimes",
                columns: new[] { "ProductId", "OperationId" });
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UretimAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderAddedDateTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Customer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Variants = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderCount = table.Column<int>(type: "int", nullable: false),
                    Carryover = table.Column<int>(type: "int", nullable: false),
                    CompletedQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AddedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastOperationId = table.Column<int>(type: "int", nullable: false),
                    AddedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.UniqueConstraint("AK_Products_ProductCode", x => x.ProductCode);
                    table.ForeignKey(
                        name: "FK_Products_Operations_LastOperationId",
                        column: x => x.LastOperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CycleTimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Second = table.Column<int>(type: "int", nullable: false),
                    AddedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleTimes_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CycleTimes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Packings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Supervisor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ExplodedFrom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExplodingTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packings_Products_ProductCode",
                        column: x => x.ProductCode,
                        principalTable: "Products",
                        principalColumn: "ProductCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionTrackingForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Line = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShiftSupervisor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Machine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SectionSupervisor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CastingDefect = table.Column<int>(type: "int", nullable: true),
                    ProcessingDefect = table.Column<int>(type: "int", nullable: true),
                    MachineFailure = table.Column<int>(type: "int", nullable: true),
                    SettingMachine = table.Column<int>(type: "int", nullable: true),
                    DiamondChange = table.Column<int>(type: "int", nullable: true),
                    RawWaiting = table.Column<int>(type: "int", nullable: true),
                    Cleaning = table.Column<int>(type: "int", nullable: true),
                    AddedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionTrackingForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionTrackingForms_Products_ProductCode",
                        column: x => x.ProductCode,
                        principalTable: "Products",
                        principalColumn: "ProductCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleTimes_OperationId",
                table: "CycleTimes",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleTimes_ProductId_OperationId",
                table: "CycleTimes",
                columns: new[] { "ProductId", "OperationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Operations_ShortCode",
                table: "Operations",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DocumentNo",
                table: "Orders",
                column: "DocumentNo");

            migrationBuilder.CreateIndex(
                name: "IX_Packings_ProductCode",
                table: "Packings",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionTrackingForms_ProductCode",
                table: "ProductionTrackingForms",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_LastOperationId",
                table: "Products",
                column: "LastOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                table: "Products",
                column: "ProductCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CycleTimes");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Packings");

            migrationBuilder.DropTable(
                name: "ProductionTrackingForms");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Operations");
        }
    }
}

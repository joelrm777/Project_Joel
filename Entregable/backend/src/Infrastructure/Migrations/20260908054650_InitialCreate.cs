using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MileageClaims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FakeDirectoryAccount",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    LinkedEmployeeNationalId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FakeDirectoryAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FakeErpEmployee",
                columns: table => new
                {
                    NationalId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverNationalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FakeErpEmployee", x => x.NationalId);
                });

            migrationBuilder.CreateTable(
                name: "MileageClaim",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNationalId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverNationalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverEmail = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    FuelType = table.Column<int>(type: "int", nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DriveType = table.Column<int>(type: "int", nullable: false),
                    ModelYear = table.Column<int>(type: "int", nullable: false),
                    EngineDisplacement = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinanceReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RemindersSent = table.Column<int>(type: "int", nullable: false),
                    LastReminderAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MileageClaim", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MileageClaimSummary",
                columns: table => new
                {
                    MileageClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeNationalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverNationalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApproverEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDistance = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    AppliedRateSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MileageClaimSummary", x => x.MileageClaimId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    MileageClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateTable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    FuelType = table.Column<int>(type: "int", nullable: false),
                    EngineDisplacementMin = table.Column<int>(type: "int", nullable: false),
                    EngineDisplacementMax = table.Column<int>(type: "int", nullable: false),
                    VehicleAgeYears = table.Column<int>(type: "int", nullable: false),
                    RatePerKm = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateTable", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Store",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Store", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreDistance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginStoreId = table.Column<int>(type: "int", nullable: false),
                    DestinationStoreId = table.Column<int>(type: "int", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(10,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreDistance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfiguration",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfiguration", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Trip",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MileageClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AppliedRatePerKm = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RateSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalDistance = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDistanceComplete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trip", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trip_MileageClaim_MileageClaimId",
                        column: x => x.MileageClaimId,
                        principalTable: "MileageClaim",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    OriginStoreId = table.Column<int>(type: "int", nullable: false),
                    DestinationStoreId = table.Column<int>(type: "int", nullable: false),
                    AppliedDistanceKm = table.Column<decimal>(type: "decimal(10,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leg_Trip_TripId",
                        column: x => x.TripId,
                        principalTable: "Trip",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FakeDirectoryAccount_Email",
                table: "FakeDirectoryAccount",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leg_TripId",
                table: "Leg",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_MileageClaim_ApproverEmail_Status",
                table: "MileageClaim",
                columns: new[] { "ApproverEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MileageClaim_EmployeeNationalId",
                table: "MileageClaim",
                column: "EmployeeNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_MileageClaimSummary_ApproverNationalId",
                table: "MileageClaimSummary",
                column: "ApproverNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_MileageClaimSummary_SubmittedAt",
                table: "MileageClaimSummary",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RateTable_VehicleType_FuelType_EngineDisplacementMin_EngineDisplacementMax_VehicleAgeYears",
                table: "RateTable",
                columns: new[] { "VehicleType", "FuelType", "EngineDisplacementMin", "EngineDisplacementMax", "VehicleAgeYears" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreDistance_OriginStoreId_DestinationStoreId",
                table: "StoreDistance",
                columns: new[] { "OriginStoreId", "DestinationStoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trip_MileageClaimId",
                table: "Trip",
                column: "MileageClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FakeDirectoryAccount");

            migrationBuilder.DropTable(
                name: "FakeErpEmployee");

            migrationBuilder.DropTable(
                name: "Leg");

            migrationBuilder.DropTable(
                name: "MileageClaimSummary");

            migrationBuilder.DropTable(
                name: "NotificationLog");

            migrationBuilder.DropTable(
                name: "RateTable");

            migrationBuilder.DropTable(
                name: "Store");

            migrationBuilder.DropTable(
                name: "StoreDistance");

            migrationBuilder.DropTable(
                name: "SystemConfiguration");

            migrationBuilder.DropTable(
                name: "Trip");

            migrationBuilder.DropTable(
                name: "MileageClaim");
        }
    }
}

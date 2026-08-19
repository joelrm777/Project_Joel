using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cine.Nucleo.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ConfiguracionTarifa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionTarifa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MontoGeneral = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MontoEstudiante = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    VigenteDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CuentaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionTarifa", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionTarifa",
                columns: new[] { "Id", "CuentaId", "MontoEstudiante", "MontoGeneral", "VigenteDesde" },
                values: new object[] { 1, null, 2500m, 3500m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionTarifa_VigenteDesde",
                table: "ConfiguracionTarifa",
                column: "VigenteDesde");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionTarifa");
        }
    }
}

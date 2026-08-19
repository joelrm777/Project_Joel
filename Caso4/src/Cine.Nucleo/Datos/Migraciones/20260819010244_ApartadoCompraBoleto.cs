using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cine.Nucleo.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ApartadoCompraBoleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Apartado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FuncionId = table.Column<int>(type: "int", nullable: false),
                    TokenSesion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VenceEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apartado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Apartado_Funcion_FuncionId",
                        column: x => x.FuncionId,
                        principalTable: "Funcion",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Compra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    FuncionId = table.Column<int>(type: "int", nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PagadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CuentaId = table.Column<int>(type: "int", nullable: true),
                    EstadoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ajustada = table.Column<bool>(type: "bit", nullable: false),
                    IngresadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClaveIdempotencia = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compra_Funcion_FuncionId",
                        column: x => x.FuncionId,
                        principalTable: "Funcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Boleto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompraId = table.Column<int>(type: "int", nullable: false),
                    FuncionId = table.Column<int>(type: "int", nullable: false),
                    Fila = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Tarifa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boleto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boleto_Compra_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OcupacionButaca_ApartadoId",
                table: "OcupacionButaca",
                column: "ApartadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Apartado_FuncionId",
                table: "Apartado",
                column: "FuncionId");

            migrationBuilder.CreateIndex(
                name: "IX_Apartado_VenceEn",
                table: "Apartado",
                column: "VenceEn");

            migrationBuilder.CreateIndex(
                name: "IX_Boleto_CompraId",
                table: "Boleto",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_ClaveIdempotencia",
                table: "Compra",
                column: "ClaveIdempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compra_Codigo",
                table: "Compra",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compra_FuncionId",
                table: "Compra",
                column: "FuncionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OcupacionButaca_Apartado_ApartadoId",
                table: "OcupacionButaca",
                column: "ApartadoId",
                principalTable: "Apartado",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OcupacionButaca_Apartado_ApartadoId",
                table: "OcupacionButaca");

            migrationBuilder.DropTable(
                name: "Apartado");

            migrationBuilder.DropTable(
                name: "Boleto");

            migrationBuilder.DropTable(
                name: "Compra");

            migrationBuilder.DropIndex(
                name: "IX_OcupacionButaca_ApartadoId",
                table: "OcupacionButaca");
        }
    }
}

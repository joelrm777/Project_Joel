using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cine.Nucleo.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class InicialCatalogoYCartelera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pelicula",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    ClasificacionEdad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pelicula", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sala",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TotalButacas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sala", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ButacaSala",
                columns: table => new
                {
                    SalaId = table.Column<int>(type: "int", nullable: false),
                    Fila = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    EsVendible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ButacaSala", x => new { x.SalaId, x.Fila, x.Numero });
                    table.ForeignKey(
                        name: "FK_ButacaSala_Sala_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Sala",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Funcion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeliculaId = table.Column<int>(type: "int", nullable: false),
                    SalaId = table.Column<int>(type: "int", nullable: false),
                    InicioLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AforoVendible = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Funcion_Pelicula_PeliculaId",
                        column: x => x.PeliculaId,
                        principalTable: "Pelicula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Funcion_Sala_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Sala",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ButacaNoVendibleFuncion",
                columns: table => new
                {
                    FuncionId = table.Column<int>(type: "int", nullable: false),
                    Fila = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ButacaNoVendibleFuncion", x => new { x.FuncionId, x.Fila, x.Numero });
                    table.ForeignKey(
                        name: "FK_ButacaNoVendibleFuncion_Funcion_FuncionId",
                        column: x => x.FuncionId,
                        principalTable: "Funcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OcupacionButaca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FuncionId = table.Column<int>(type: "int", nullable: false),
                    Fila = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApartadoId = table.Column<int>(type: "int", nullable: true),
                    BoletoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcupacionButaca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcupacionButaca_Funcion_FuncionId",
                        column: x => x.FuncionId,
                        principalTable: "Funcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Pelicula",
                columns: new[] { "Id", "ClasificacionEdad", "DuracionMinutos", "Titulo" },
                values: new object[,]
                {
                    { 1, 0, 100, "El Último Autobús" },
                    { 2, 12, 120, "Noche de Tormenta" },
                    { 3, 16, 95, "Camino al Volcán" }
                });

            migrationBuilder.InsertData(
                table: "Sala",
                columns: new[] { "Id", "Nombre", "TotalButacas" },
                values: new object[,]
                {
                    { 1, "Sala 1", 120 },
                    { 2, "Sala 2", 60 }
                });

            migrationBuilder.InsertData(
                table: "ButacaSala",
                columns: new[] { "Fila", "Numero", "SalaId", "EsVendible" },
                values: new object[,]
                {
                    { "A", 1, 1, false },
                    { "A", 2, 1, true },
                    { "A", 3, 1, true },
                    { "A", 4, 1, true },
                    { "A", 5, 1, true },
                    { "A", 6, 1, true },
                    { "A", 7, 1, true },
                    { "A", 8, 1, true },
                    { "A", 9, 1, true },
                    { "A", 10, 1, true },
                    { "A", 11, 1, true },
                    { "A", 12, 1, false },
                    { "B", 1, 1, true },
                    { "B", 2, 1, true },
                    { "B", 3, 1, true },
                    { "B", 4, 1, true },
                    { "B", 5, 1, true },
                    { "B", 6, 1, true },
                    { "B", 7, 1, true },
                    { "B", 8, 1, true },
                    { "B", 9, 1, true },
                    { "B", 10, 1, true },
                    { "B", 11, 1, true },
                    { "B", 12, 1, true },
                    { "C", 1, 1, true },
                    { "C", 2, 1, true },
                    { "C", 3, 1, true },
                    { "C", 4, 1, true },
                    { "C", 5, 1, true },
                    { "C", 6, 1, true },
                    { "C", 7, 1, true },
                    { "C", 8, 1, true },
                    { "C", 9, 1, true },
                    { "C", 10, 1, true },
                    { "C", 11, 1, true },
                    { "C", 12, 1, true },
                    { "D", 1, 1, true },
                    { "D", 2, 1, true },
                    { "D", 3, 1, true },
                    { "D", 4, 1, true },
                    { "D", 5, 1, true },
                    { "D", 6, 1, true },
                    { "D", 7, 1, true },
                    { "D", 8, 1, true },
                    { "D", 9, 1, true },
                    { "D", 10, 1, true },
                    { "D", 11, 1, true },
                    { "D", 12, 1, true },
                    { "E", 1, 1, true },
                    { "E", 2, 1, true },
                    { "E", 3, 1, true },
                    { "E", 4, 1, true },
                    { "E", 5, 1, true },
                    { "E", 6, 1, true },
                    { "E", 7, 1, true },
                    { "E", 8, 1, true },
                    { "E", 9, 1, true },
                    { "E", 10, 1, true },
                    { "E", 11, 1, true },
                    { "E", 12, 1, true },
                    { "F", 1, 1, true },
                    { "F", 2, 1, true },
                    { "F", 3, 1, true },
                    { "F", 4, 1, true },
                    { "F", 5, 1, true },
                    { "F", 6, 1, true },
                    { "F", 7, 1, true },
                    { "F", 8, 1, true },
                    { "F", 9, 1, true },
                    { "F", 10, 1, true },
                    { "F", 11, 1, true },
                    { "F", 12, 1, true },
                    { "G", 1, 1, true },
                    { "G", 2, 1, true },
                    { "G", 3, 1, true },
                    { "G", 4, 1, true },
                    { "G", 5, 1, true },
                    { "G", 6, 1, true },
                    { "G", 7, 1, true },
                    { "G", 8, 1, true },
                    { "G", 9, 1, true },
                    { "G", 10, 1, true },
                    { "G", 11, 1, true },
                    { "G", 12, 1, true },
                    { "H", 1, 1, true },
                    { "H", 2, 1, true },
                    { "H", 3, 1, true },
                    { "H", 4, 1, true },
                    { "H", 5, 1, true },
                    { "H", 6, 1, true },
                    { "H", 7, 1, true },
                    { "H", 8, 1, true },
                    { "H", 9, 1, true },
                    { "H", 10, 1, true },
                    { "H", 11, 1, true },
                    { "H", 12, 1, true },
                    { "I", 1, 1, true },
                    { "I", 2, 1, true },
                    { "I", 3, 1, true },
                    { "I", 4, 1, true },
                    { "I", 5, 1, true },
                    { "I", 6, 1, true },
                    { "I", 7, 1, true },
                    { "I", 8, 1, true },
                    { "I", 9, 1, true },
                    { "I", 10, 1, true },
                    { "I", 11, 1, true },
                    { "I", 12, 1, true },
                    { "J", 1, 1, true },
                    { "J", 2, 1, true },
                    { "J", 3, 1, true },
                    { "J", 4, 1, true },
                    { "J", 5, 1, true },
                    { "J", 6, 1, false },
                    { "J", 7, 1, true },
                    { "J", 8, 1, true },
                    { "J", 9, 1, true },
                    { "J", 10, 1, true },
                    { "J", 11, 1, true },
                    { "J", 12, 1, true },
                    { "A", 1, 2, true },
                    { "A", 2, 2, true },
                    { "A", 3, 2, true },
                    { "A", 4, 2, true },
                    { "A", 5, 2, true },
                    { "A", 6, 2, true },
                    { "A", 7, 2, true },
                    { "A", 8, 2, true },
                    { "A", 9, 2, true },
                    { "A", 10, 2, true },
                    { "A", 11, 2, true },
                    { "A", 12, 2, true },
                    { "B", 1, 2, true },
                    { "B", 2, 2, true },
                    { "B", 3, 2, true },
                    { "B", 4, 2, true },
                    { "B", 5, 2, true },
                    { "B", 6, 2, true },
                    { "B", 7, 2, true },
                    { "B", 8, 2, true },
                    { "B", 9, 2, true },
                    { "B", 10, 2, true },
                    { "B", 11, 2, true },
                    { "B", 12, 2, true },
                    { "C", 1, 2, true },
                    { "C", 2, 2, true },
                    { "C", 3, 2, true },
                    { "C", 4, 2, true },
                    { "C", 5, 2, true },
                    { "C", 6, 2, true },
                    { "C", 7, 2, true },
                    { "C", 8, 2, true },
                    { "C", 9, 2, true },
                    { "C", 10, 2, true },
                    { "C", 11, 2, true },
                    { "C", 12, 2, true },
                    { "D", 1, 2, true },
                    { "D", 2, 2, true },
                    { "D", 3, 2, true },
                    { "D", 4, 2, true },
                    { "D", 5, 2, true },
                    { "D", 6, 2, true },
                    { "D", 7, 2, true },
                    { "D", 8, 2, true },
                    { "D", 9, 2, true },
                    { "D", 10, 2, true },
                    { "D", 11, 2, true },
                    { "D", 12, 2, true },
                    { "E", 1, 2, true },
                    { "E", 2, 2, true },
                    { "E", 3, 2, true },
                    { "E", 4, 2, true },
                    { "E", 5, 2, true },
                    { "E", 6, 2, true },
                    { "E", 7, 2, true },
                    { "E", 8, 2, true },
                    { "E", 9, 2, true },
                    { "E", 10, 2, true },
                    { "E", 11, 2, true },
                    { "E", 12, 2, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Funcion_PeliculaId",
                table: "Funcion",
                column: "PeliculaId");

            migrationBuilder.CreateIndex(
                name: "IX_Funcion_SalaId_InicioLocal",
                table: "Funcion",
                columns: new[] { "SalaId", "InicioLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_OcupacionButaca_FuncionId_Fila_Numero",
                table: "OcupacionButaca",
                columns: new[] { "FuncionId", "Fila", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ButacaNoVendibleFuncion");

            migrationBuilder.DropTable(
                name: "ButacaSala");

            migrationBuilder.DropTable(
                name: "OcupacionButaca");

            migrationBuilder.DropTable(
                name: "Funcion");

            migrationBuilder.DropTable(
                name: "Pelicula");

            migrationBuilder.DropTable(
                name: "Sala");
        }
    }
}

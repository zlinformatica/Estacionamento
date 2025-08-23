using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estacionamento.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposPermanencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "ValorPago",
                table: "Movimentos",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Permanencia",
                table: "Movimentos",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorInicial",
                table: "Movimentos",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permanencia",
                table: "Movimentos");

            migrationBuilder.DropColumn(
                name: "ValorInicial",
                table: "Movimentos");

            migrationBuilder.AlterColumn<decimal>(
                name: "ValorPago",
                table: "Movimentos",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");
        }
    }
}

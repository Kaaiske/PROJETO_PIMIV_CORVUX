using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorvusIAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddChamadoWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "ChamadosSuporte",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataAtualizacao",
                table: "ChamadosSuporte",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DataResolucao",
                table: "ChamadosSuporte",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrazoSla",
                table: "ChamadosSuporte",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prioridade",
                table: "ChamadosSuporte",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ResponsavelId",
                table: "ChamadosSuporte",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Solucao",
                table: "ChamadosSuporte",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ChamadosSuporte_ResponsavelId",
                table: "ChamadosSuporte",
                column: "ResponsavelId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChamadosSuporte_Usuarios_ResponsavelId",
                table: "ChamadosSuporte",
                column: "ResponsavelId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChamadosSuporte_Usuarios_ResponsavelId",
                table: "ChamadosSuporte");

            migrationBuilder.DropIndex(
                name: "IX_ChamadosSuporte_ResponsavelId",
                table: "ChamadosSuporte");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "ChamadosSuporte");

            migrationBuilder.DropColumn(
                name: "DataAtualizacao",
                table: "ChamadosSuporte");

            migrationBuilder.DropColumn(
                name: "DataResolucao",
                table: "ChamadosSuporte");

            migrationBuilder.DropColumn(
                name: "PrazoSla",
                table: "ChamadosSuporte");

            migrationBuilder.DropColumn(
                name: "Prioridade",
                table: "ChamadosSuporte");

            migrationBuilder.DropColumn(
                name: "ResponsavelId",
                table: "ChamadosSuporte");

            migrationBuilder.DropColumn(
                name: "Solucao",
                table: "ChamadosSuporte");
        }
    }
}

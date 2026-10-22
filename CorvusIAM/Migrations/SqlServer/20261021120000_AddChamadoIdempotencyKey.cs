using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using CorvusIAM.Data;

#nullable disable

namespace CorvusIAM.Migrations.SqlServer
{
    [DbContext(typeof(SqlServerDbContext))]
    [Migration("20261021120000_AddChamadoIdempotencyKey")]
    public partial class AddChamadoIdempotencyKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>("ClientIdempotencyKey", "ChamadosSuporte", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.CreateIndex("IX_ChamadosSuporte_UsuarioId_ClientIdempotencyKey", "ChamadosSuporte", new[] { "UsuarioId", "ClientIdempotencyKey" }, unique: true, filter: "[ClientIdempotencyKey] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("IX_ChamadosSuporte_UsuarioId_ClientIdempotencyKey", "ChamadosSuporte");
            migrationBuilder.DropColumn("ClientIdempotencyKey", "ChamadosSuporte");
        }
    }
}

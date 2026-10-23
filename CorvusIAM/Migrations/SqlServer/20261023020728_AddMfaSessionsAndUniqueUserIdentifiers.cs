using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorvusIAM.Migrations.SqlServer;

public partial class AddMfaSessionsAndUniqueUserIdentifiers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MfaSessoes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                UsuarioId = table.Column<int>(type: "int", nullable: false),
                Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ExpiraEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                Tentativas = table.Column<int>(type: "int", nullable: false),
                Canal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MfaSessoes", x => x.Id);
                table.ForeignKey("FK_MfaSessoes_Usuarios_UsuarioId", x => x.UsuarioId, "Usuarios", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MfaSessoes_TokenHash' AND object_id = OBJECT_ID('MfaSessoes')) CREATE UNIQUE INDEX IX_MfaSessoes_TokenHash ON MfaSessoes(TokenHash);");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MfaSessoes_UsuarioId' AND object_id = OBJECT_ID('MfaSessoes')) CREATE INDEX IX_MfaSessoes_UsuarioId ON MfaSessoes(UsuarioId);");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_Email' AND object_id = OBJECT_ID('Usuarios')) CREATE UNIQUE INDEX IX_Usuarios_Email ON Usuarios(Email);");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_CPF' AND object_id = OBJECT_ID('Usuarios')) CREATE UNIQUE INDEX IX_Usuarios_CPF ON Usuarios(CPF) WHERE CPF IS NOT NULL AND CPF <> '';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("MfaSessoes");
        migrationBuilder.DropIndex("IX_Usuarios_Email", "Usuarios");
        migrationBuilder.DropIndex("IX_Usuarios_CPF", "Usuarios");
    }
}

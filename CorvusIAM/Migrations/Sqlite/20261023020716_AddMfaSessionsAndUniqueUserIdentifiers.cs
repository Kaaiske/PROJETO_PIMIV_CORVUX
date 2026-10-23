using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorvusIAM.Migrations.Sqlite;

public partial class AddMfaSessionsAndUniqueUserIdentifiers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MfaSessoes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                TokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                Tentativas = table.Column<int>(type: "INTEGER", nullable: false),
                Canal = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MfaSessoes", x => x.Id);
                table.ForeignKey("FK_MfaSessoes_Usuarios_UsuarioId", x => x.UsuarioId, "Usuarios", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_MfaSessoes_TokenHash ON MfaSessoes(TokenHash);");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_MfaSessoes_UsuarioId ON MfaSessoes(UsuarioId);");
        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_Usuarios_Email ON Usuarios(Email);");
        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_Usuarios_CPF ON Usuarios(CPF) WHERE CPF IS NOT NULL AND CPF <> '';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("MfaSessoes");
        migrationBuilder.DropIndex("IX_Usuarios_Email", "Usuarios");
        migrationBuilder.DropIndex("IX_Usuarios_CPF", "Usuarios");
    }
}

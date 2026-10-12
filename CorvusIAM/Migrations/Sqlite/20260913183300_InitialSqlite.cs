using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CorvusIAM.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Chave = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perfis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoliticasAcesso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasAcesso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    TentativasLogin = table.Column<int>(type: "INTEGER", nullable: false),
                    DataDesbloqueio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResetSenhaToken = table.Column<string>(type: "TEXT", nullable: false),
                    ResetSenhaExpiracao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CPF = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Cargo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DataContratacao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RG = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Departamento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Endereco = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfilPermissoes",
                columns: table => new
                {
                    PerfilId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissaoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilPermissoes", x => new { x.PerfilId, x.PermissaoId });
                    table.ForeignKey(
                        name: "FK_PerfilPermissoes_Perfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerfilPermissoes_Permissoes_PermissaoId",
                        column: x => x.PermissaoId,
                        principalTable: "Permissoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChamadosSuporte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Assunto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DataAbertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataFechamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChamadosSuporte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChamadosSuporte_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    DataHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Acao = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Detalhe = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Origem = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsAuditoria_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPerfis",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    PerfilId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPerfis", x => new { x.UsuarioId, x.PerfilId });
                    table.ForeignKey(
                        name: "FK_UsuarioPerfis_Perfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioPerfis_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Perfis",
                columns: new[] { "Id", "Descricao", "Nome" },
                values: new object[,]
                {
                    { 1, "Administrador do Sistema", "Admin" },
                    { 2, "Recursos Humanos (RH)", "Recursos Humanos" },
                    { 3, "Contabilidade", "Contabilidade" },
                    { 4, "Suporte de TI / Help Desk", "Help Desk" },
                    { 5, "Usuário Padrão", "Colaborador" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Ativo", "CPF", "Cargo", "DataContratacao", "DataDesbloqueio", "DataNascimento", "Departamento", "Email", "Endereco", "Nome", "RG", "ResetSenhaExpiracao", "ResetSenhaToken", "SenhaHash", "Telefone", "TentativasLogin" },
                values: new object[] { 1, true, "000.000.000-00", "CTO", new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tecnologia", "admin@corvus.com", "Av. Paulista, 1000 - SP", "Administrador TI", "00.000.000-0", null, "", "$2a$11$y4AkJtTDlULmPEEKkqBLk.MM4ZKyMK8ExkIkfyAY5s/iEMDL1UvIS", "(11) 99999-9999", 0 });

            migrationBuilder.InsertData(
                table: "UsuarioPerfis",
                columns: new[] { "PerfilId", "UsuarioId" },
                values: new object[] { 1, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_ChamadosSuporte_UsuarioId",
                table: "ChamadosSuporte",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_UsuarioId",
                table: "LogsAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilPermissoes_PermissaoId",
                table: "PerfilPermissoes",
                column: "PermissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPerfis_PerfilId",
                table: "UsuarioPerfis",
                column: "PerfilId");

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_AuditoriaUsuarios_Insert
AFTER INSERT ON Usuarios
BEGIN
    INSERT INTO LogsAuditoria (DataHora, Acao, Detalhe, Tipo, Origem)
    VALUES (datetime('now'), 'Alteração em Usuários', 'Modificação detectada pelo banco', 'Trigger', 'DB Trigger');
END;
");
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_AuditoriaUsuarios_Update
AFTER UPDATE ON Usuarios
BEGIN
    INSERT INTO LogsAuditoria (DataHora, Acao, Detalhe, Tipo, Origem)
    VALUES (datetime('now'), 'Alteração em Usuários', 'Modificação detectada pelo banco', 'Trigger', 'DB Trigger');
END;
");
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_AuditoriaUsuarios_Delete
AFTER DELETE ON Usuarios
BEGIN
    INSERT INTO LogsAuditoria (DataHora, Acao, Detalhe, Tipo, Origem)
    VALUES (datetime('now'), 'Alteração em Usuários', 'Modificação detectada pelo banco', 'Trigger', 'DB Trigger');
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_AuditoriaUsuarios_Insert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_AuditoriaUsuarios_Update");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_AuditoriaUsuarios_Delete");

            migrationBuilder.DropTable(
                name: "ChamadosSuporte");

            migrationBuilder.DropTable(
                name: "ConfiguracoesSistema");

            migrationBuilder.DropTable(
                name: "LogsAuditoria");

            migrationBuilder.DropTable(
                name: "PerfilPermissoes");

            migrationBuilder.DropTable(
                name: "PoliticasAcesso");

            migrationBuilder.DropTable(
                name: "UsuarioPerfis");

            migrationBuilder.DropTable(
                name: "Permissoes");

            migrationBuilder.DropTable(
                name: "Perfis");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}


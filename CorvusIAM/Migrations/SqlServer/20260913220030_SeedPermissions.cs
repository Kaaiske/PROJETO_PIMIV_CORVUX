using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CorvusIAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class SeedPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissoes",
                columns: new[] { "Id", "Descricao", "Nome" },
                values: new object[,]
                {
                    { 1, "Visualizar usuários", "usuarios.visualizar" },
                    { 2, "Gerenciar usuários", "usuarios.gerenciar" },
                    { 3, "Gerenciar perfis", "perfis.gerenciar" },
                    { 4, "Atender chamados", "chamados.atender" },
                    { 5, "Visualizar auditoria", "logs.visualizar" },
                    { 6, "Gerenciar políticas", "politicas.gerenciar" }
                });

            migrationBuilder.InsertData(
                table: "PerfilPermissoes",
                columns: new[] { "PerfilId", "PermissaoId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 1, 3 },
                    { 1, 4 },
                    { 1, 5 },
                    { 1, 6 },
                    { 2, 1 },
                    { 2, 2 },
                    { 4, 4 },
                    { 5, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 1, 4 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 1, 5 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 1, 6 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 2, 2 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 4, 4 });

            migrationBuilder.DeleteData(
                table: "PerfilPermissoes",
                keyColumns: new[] { "PerfilId", "PermissaoId" },
                keyValues: new object[] { 5, 1 });

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Permissoes",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}

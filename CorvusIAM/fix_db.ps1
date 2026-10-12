using namespace Microsoft.Data.Sqlite

$connectionString = "Data Source=corvusiam.db"
$connection = [Microsoft.Data.Sqlite.SqliteConnection]::new($connectionString)
$connection.Open()

$command = $connection.CreateCommand()
$command.CommandText = @"
ALTER TABLE Usuarios ADD COLUMN DataDesbloqueio TEXT NULL;
ALTER TABLE Usuarios ADD COLUMN TentativasLogin INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Usuarios ADD COLUMN ResetSenhaToken TEXT NULL;
ALTER TABLE Usuarios ADD COLUMN ResetSenhaExpiracao TEXT NULL;
"@
try {
    $command.ExecuteNonQuery()
    Write-Host "Columns added successfully."
} catch {
    Write-Host "Error or columns already exist: $_"
}

$connection.Close()

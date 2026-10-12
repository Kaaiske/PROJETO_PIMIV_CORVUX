$file = "C:\Users\Kaiiske\Desktop\CorvusIAM-20260830T010159Z-1-001\CorvusIAM\Controllers\AdminController.cs"
$content = Get-Content $file -Raw

# Replace class level authorize
$content = $content -replace '\[Authorize\(Policy = "RequireAdmin"\)\]\s*public class AdminController', '[Authorize] public class AdminController'

# Add RequireAdmin to specific actions
$adminActions = @("public IActionResult Index()", "public IActionResult Politicas()", "public IActionResult Cloud()", "public IActionResult Logs()", "public IActionResult Inteligencia()", "public IActionResult Configuracoes()", "public IActionResult DownloadRelatorio", "public IActionResult Sincronizar")
foreach ($action in $adminActions) {
    $content = $content.Replace($action, "[Authorize(Policy = `"RequireAdmin`")]`r`n        $action")
}

# Add RequireHR to user management actions
$hrActions = @("public IActionResult Usuarios()", "public async System.Threading.Tasks.Task<IActionResult> AdicionarUsuario", "public async System.Threading.Tasks.Task<IActionResult> ResetarSenhaUsuario", "public IActionResult EditarUsuario", "public IActionResult InativarUsuario", "public IActionResult ExcluirUsuario")
foreach ($action in $hrActions) {
    $content = $content.Replace($action, "[Authorize(Policy = `"RequireHR`")]`r`n        $action")
}

# Add RequireAdminOrHelpDesk to Chamados
$helpDeskActions = @("public IActionResult Chamados()")
foreach ($action in $helpDeskActions) {
    $content = $content.Replace($action, "[Authorize(Policy = `"RequireAdminOrHelpDesk`")]`r`n        $action")
}

Set-Content -Path $file -Value $content

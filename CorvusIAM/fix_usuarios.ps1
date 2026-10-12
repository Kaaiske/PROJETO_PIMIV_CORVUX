$file = "C:\Users\Kaiiske\Desktop\CorvusIAM-20260830T010159Z-1-001\CorvusIAM\Views\Admin\Usuarios.cshtml"
$content = Get-Content $file -Raw

# Insert Antiforgery token at the top of the body/content section
$content = $content -replace '<div class="dashboard-container">', '<div class="dashboard-container">@Html.AntiForgeryToken()'

# Update all fetch calls to include the token in headers
$fetchPattern = 'headers:\s*\{\s*''Content-Type'':\s*''application/json''\s*\}'
$fetchReplacement = "headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': document.querySelector('input[name=`"__RequestVerificationToken`"]').value }"
$content = [System.Text.RegularExpressions.Regex]::Replace($content, $fetchPattern, $fetchReplacement)

Set-Content -Path $file -Value $content

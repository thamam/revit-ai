# Set Claude API Key
# Replace YOUR_API_KEY_HERE with your actual Claude API key

$apiKey = Read-Host "Enter your Claude API key (starts with sk-ant-)"

if ($apiKey) {
    [Environment]::SetEnvironmentVariable("CLAUDE_API_KEY", $apiKey, "User")
    Write-Host ""
    Write-Host "API key has been set!" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: You must RESTART REVIT for the change to take effect." -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "No API key entered. Cancelled." -ForegroundColor Red
}

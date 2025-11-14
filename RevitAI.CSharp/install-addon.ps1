$appData = [Environment]::GetFolderPath('ApplicationData')
$dest = Join-Path $appData 'Autodesk\Revit\Addins\2026'
$revitAIFolder = Join-Path $dest 'RevitAI'

# Create folder
New-Item -ItemType Directory -Path $revitAIFolder -Force | Out-Null

# Copy DLL files
$sourceFiles = 'C:\Users\nelly\Documents\projects\revit-ai\RevitAI.CSharp\bin\Release\*'
Copy-Item $sourceFiles -Destination $revitAIFolder -Recurse -Force

# Copy manifest
$manifestSource = 'C:\Users\nelly\Documents\projects\revit-ai\RevitAI.CSharp\RevitAI.addin'
Copy-Item $manifestSource -Destination $dest -Force

Write-Host "Installation complete!"
Write-Host "DLL files copied to: $revitAIFolder"
Write-Host "Manifest copied to: $dest"

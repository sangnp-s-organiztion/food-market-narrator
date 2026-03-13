# Merge all files in Services into one text file.
# Run: powershell -ExecutionPolicy Bypass -File .\merge-text.ps1

$projectRoot = $PSScriptRoot
$servicesPath = Join-Path $projectRoot "Services"
$outputPath = Join-Path $projectRoot "merge-text.txt"

if (-not (Test-Path $servicesPath)) {
    Write-Error "Khong tim thay thu muc Services tai: $servicesPath"
    exit 1
}

$serviceFiles = Get-ChildItem -Path $servicesPath -File | Sort-Object Name

if (-not $serviceFiles) {
    # Overwrite with empty content if no files are found.
    Set-Content -Path $outputPath -Value "" -Encoding UTF8
    Write-Host "Khong co file nao trong Services. Da ghi de file rong: $outputPath"
    exit 0
}

$mergedParts = foreach ($file in $serviceFiles) {
    $header = "===== $($file.Name) ====="
    $content = Get-Content -Path $file.FullName -Raw
    "$header`r`n$content"
}

$finalContent = $mergedParts -join "`r`n`r`n"

# Always overwrite previous output.
Set-Content -Path $outputPath -Value $finalContent -Encoding UTF8

Write-Host "Da gop $($serviceFiles.Count) file vao: $outputPath"

# ===== 0. Set env + flag =====
$env:CLAUDE_CODE_MAX_OUTPUT_TOKENS = "64000"

# ===== 1. Load core files =====
$coreContent = @()

# file chính
if (Test-Path ".\CLAUDE.md") {
    $coreContent += Get-Content ".\CLAUDE.md" -Encoding UTF8
}

# docs (recursive)
if (Test-Path ".\docs") {
    $coreContent += Get-ChildItem ".\docs" -Filter *.md -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object { Get-Content $_.FullName -Encoding UTF8 }
}

# .claude (recursive)
if (Test-Path ".\.claude") {
    $coreContent += Get-ChildItem ".\.claude" -Filter *.md -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object { Get-Content $_.FullName -Encoding UTF8 }
}

# ===== 2. Feature =====
$featureContent = @()

if (Test-Path ".\docs\feature-requirment") {
    $featureContent += Get-ChildItem ".\docs\feature-requirment" -Filter *.md -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object { Get-Content $_.FullName -Encoding UTF8 }
}

# ===== 3. Optional =====
$optionalContent = @()
$optionalFolders = @(".\docs\maui", ".\docs\saler")

foreach ($folder in $optionalFolders) {
    if (Test-Path $folder) {
        $optionalContent += Get-ChildItem $folder -Filter *.md -ErrorAction SilentlyContinue |
            ForEach-Object { Get-Content $_.FullName -Encoding UTF8 }
    }
}

# ===== 4. Context =====
$context = @(
    $coreContent
    $featureContent
    $optionalContent
) -join "`n`n"

# ===== 5. Prompt =====
Write-Host "Enter ur prompt:" -ForegroundColor Cyan
$prompt = Read-Host

# ===== 6. Full input =====
$fullInput = @"
You are working on this project.

==== PROJECT CONTEXT ====
$context

==== USER REQUEST ====
$prompt
"@

# ===== 7. Run Claude =====
$fullInput | claude --dangerously-skip-permissions
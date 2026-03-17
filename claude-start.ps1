# ===== 0. ENV =====
$env:CLAUDE_CODE_MAX_OUTPUT_TOKENS = "64000"

# ===== 1. CHỌN MODE =====
Write-Host "Choose mode:" -ForegroundColor Cyan
Write-Host "1. Backend (.NET WebAPI)"
Write-Host "2. Frontend (.NET MAUI)"

$choice = Read-Host "Enter your choice (1 or 2)"

# ===== 2. LOAD CORE =====
$coreContent = @()

if (Test-Path ".\CLAUDE.MD") {
    $coreContent += Get-Content ".\CLAUDE.MD" -Encoding UTF8
}

if (Test-Path ".\docs\prd.md") {
    $coreContent += Get-Content ".\docs\prd.md" -Encoding UTF8
}

# ===== 3. LOAD THEO MODE =====
$modeContent = @()

if ($choice -eq "1") {
    Write-Host "Mode: Backend" -ForegroundColor Yellow

    # feature requirements
    if (Test-Path ".\docs\feature-requirment") {
        $modeContent += Get-ChildItem ".\docs\feature-requirment" -Recurse -Filter *.md |
            ForEach-Object { Get-Content $_.FullName -Encoding UTF8 }
    }

    # saler (backend logic)
    if (Test-Path ".\docs\saler") {
        $modeContent += Get-ChildItem ".\docs\saler" -Filter *.md |
            ForEach-Object { Get-Content $_.FullName -Encoding UTF8 }
    }

    $systemNote = "You are a senior .NET backend developer using ASP.NET Core WebAPI."
}

elseif ($choice -eq "2") {
    Write-Host "Mode: Frontend (MAUI)" -ForegroundColor Yellow

    # maui docs
    if (Test-Path ".\docs\maui") {
        $modeContent += Get-ChildItem ".\docs\maui" -Filter *.md |
            ForEach-Object { Get-Content $_.FullName -Encoding UTF8 }
    }

    # root maui docs
    if (Test-Path ".\maui-theory.md") {
        $modeContent += Get-Content ".\maui-theory.md" -Encoding UTF8
    }

    if (Test-Path ".\maui-ui-cheatsheet.md") {
        $modeContent += Get-Content ".\maui-ui-cheatsheet.md" -Encoding UTF8
    }

    $systemNote = "You are a senior .NET MAUI developer using XAML and code-behind (.xaml.cs)."
}

else {
    Write-Host "Invalid choose" -ForegroundColor Red
    exit
}

# ===== 4. CONTEXT =====
$context = @(
    $coreContent
    $modeContent
) -join "`n`n"

# ===== 5. NHẬP PROMPT =====
Write-Host "Enter ur promt: " -ForegroundColor Cyan
$prompt = Read-Host

# ===== 6. FULL INPUT =====
$fullInput = @"
$systemNote

==== PROJECT CONTEXT ====
$context

==== USER REQUEST ====
$prompt
"@

# ===== 7. RUN =====
$fullInput | claude --dangerously-skip-permissions
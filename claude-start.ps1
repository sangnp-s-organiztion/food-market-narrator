# Move to project root (chỉnh path cho đúng)
Set-Location "D:\Program\food-market-narrator"

# Set max tokens
$env:CLAUDE_CODE_MAX_OUTPUT_TOKENS = "64000"

# Read CLAUDE.md
$claudeMd = Get-Content -Raw -Path "CLAUDE.md"

# Read all files in .claude folder
$claudeFolder = ""
if (Test-Path ".claude") {
    $files = Get-ChildItem ".claude" -File
    foreach ($file in $files) {
        $content = Get-Content -Raw $file.FullName
        $claudeFolder += "`n--- FILE: $($file.Name) ---`n$content`n"
    }
}

# Combine context
$context = @"
You must follow this project context strictly:

===== CLAUDE.MD =====
$claudeMd

===== .CLAUDE FILES =====
$claudeFolder

Always follow these rules before doing anything.
"@

# Start Claude with injected context
claude --dangerously-skip-permissions "$context"

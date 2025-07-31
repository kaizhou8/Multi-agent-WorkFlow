# Final cleanup script for remaining Chinese text in Multi-Agent project
Write-Host "Starting final Chinese text cleanup..." -ForegroundColor Green

$projectRoot = "C:\Users\phoen\Documents\wind\Multi-Agent"

# Function to clean remaining Chinese patterns
function Clean-RemainingChinese {
    param([string]$filePath)
    
    $content = Get-Content $filePath -Raw -Encoding UTF8
    $originalContent = $content
    
    # Fix specific patterns found in the search results
    
    # Pattern: "（秒）" -> ""
    $content = $content -replace '（秒）', ''
    
    # Pattern: "CPU使用率" -> ""
    $content = $content -replace 'CPU使用率', ''
    
    # Pattern: "IDID获取" -> "ID to get"
    $content = $content -replace 'IDID获取[^<]*', 'ID to get'
    
    # Pattern: "AI服务接口" -> ""
    $content = $content -replace 'AI服务接口', ''
    
    # Pattern: "AI模型生成响应" -> ""
    $content = $content -replace 'AI模型生成响应', ''
    
    # Pattern: "AI响应" -> "AI response"
    $content = $content -replace 'AI响应', 'AI response'
    
    # Pattern: "AI模型" -> "AI model"
    $content = $content -replace 'AI模型', 'AI model'
    
    # Pattern: "AI模型信息" -> ""
    $content = $content -replace 'AI模型信息', ''
    
    # Pattern: "AI分析结果" -> ""
    $content = $content -replace 'AI分析结果', ''
    
    # Clean XAML comments with Chinese
    $content = $content -replace '<!-- .* / [^>]*-->', '<!-- $1 -->'
    $content = $content -replace '<!-- [^/]* / ([^>]*) -->', '<!-- $1 -->'
    
    # Clean Text attributes with Chinese
    $content = $content -replace 'Text="[^"]*/ ([^"]*)"', 'Text="$1"'
    $content = $content -replace 'Text="([^"]*) / [^"]*"', 'Text="$1"'
    
    # Clean EmptyView with Chinese
    $content = $content -replace 'EmptyView="[^"]*/ ([^"]*)"', 'EmptyView="$1"'
    $content = $content -replace 'EmptyView="([^"]*) / [^"]*"', 'EmptyView="$1"'
    
    # Remove any remaining standalone Chinese characters/words
    $content = $content -replace '[\u4e00-\u9fff]+', ''
    
    # Clean up extra spaces and empty lines
    $content = $content -replace '\s+', ' '
    $content = $content -replace '^\s*$\n', ''
    $content = $content -replace '\r?\n\s*\r?\n\s*\r?\n', "`r`n`r`n"
    
    if ($content -ne $originalContent) {
        Set-Content $filePath $content -Encoding UTF8
        Write-Host "Final cleanup: $filePath" -ForegroundColor Yellow
        return $true
    }
    return $false
}

# Process all files that might still contain Chinese
$filePatterns = @("*.cs", "*.xaml")
$totalFiles = 0
$modifiedFiles = 0

foreach ($pattern in $filePatterns) {
    $files = Get-ChildItem -Path $projectRoot -Filter $pattern -Recurse | Where-Object { 
        $_.FullName -notlike "*\bin\*" -and 
        $_.FullName -notlike "*\obj\*" -and
        $_.FullName -notlike "*\.git\*"
    }
    
    foreach ($file in $files) {
        $totalFiles++
        if (Clean-RemainingChinese $file.FullName) {
            $modifiedFiles++
        }
    }
}

Write-Host "`nFinal Chinese cleanup completed!" -ForegroundColor Green
Write-Host "Total files processed: $totalFiles" -ForegroundColor Cyan
Write-Host "Files modified: $modifiedFiles" -ForegroundColor Cyan

# PowerShell script to remove Chinese text from Multi-Agent project
# This script will systematically clean all Chinese text from code files

Write-Host "Starting Chinese text removal from Multi-Agent project..." -ForegroundColor Green

# Define file patterns to process
$filePatterns = @("*.cs", "*.xaml", "*.md", "*.json", "*.xml")
$projectRoot = "C:\Users\phoen\Documents\wind\Multi-Agent"

# Function to remove Chinese text from bilingual comments
function Remove-ChineseFromFile {
    param([string]$filePath)
    
    $content = Get-Content $filePath -Raw -Encoding UTF8
    $originalContent = $content
    
    # Remove Chinese from XML documentation comments
    # Pattern: /// <summary>English / 中文</summary>
    $content = $content -replace '(<summary>.*?) / [\u4e00-\u9fff]+', '$1'
    $content = $content -replace '(<param.*?>.*?) / [\u4e00-\u9fff]+', '$1'
    $content = $content -replace '(<returns>.*?) / [\u4e00-\u9fff]+', '$1'
    $content = $content -replace '(<remarks>.*?) / [\u4e00-\u9fff]+', '$1'
    
    # Remove Chinese from single-line comments
    # Pattern: // English / 中文
    $content = $content -replace '(//.*?) / [\u4e00-\u9fff]+', '$1'
    
    # Remove Chinese from multi-line comments
    # Pattern: /* English / 中文 */
    $content = $content -replace '(/\*.*?) / [\u4e00-\u9fff]+(\s*\*/)', '$1$2'
    
    # Remove standalone Chinese comments
    $content = $content -replace '//\s*[\u4e00-\u9fff]+.*\r?\n', ''
    $content = $content -replace '/\*\s*[\u4e00-\u9fff]+.*?\*/', ''
    
    # Remove Chinese from string literals (be careful with this)
    # Only remove obvious UI text patterns
    $content = $content -replace '"[\u4e00-\u9fff]+"', '""'
    
    # Clean up extra whitespace
    $content = $content -replace '\r?\n\s*\r?\n\s*\r?\n', "`r`n`r`n"
    
    if ($content -ne $originalContent) {
        Set-Content $filePath $content -Encoding UTF8
        Write-Host "Cleaned: $filePath" -ForegroundColor Yellow
        return $true
    }
    return $false
}

# Process all files
$totalFiles = 0
$modifiedFiles = 0

foreach ($pattern in $filePatterns) {
    $files = Get-ChildItem -Path $projectRoot -Filter $pattern -Recurse | Where-Object { 
        $_.FullName -notlike "*\bin\*" -and 
        $_.FullName -notlike "*\obj\*" -and
        $_.FullName -notlike "*\.git\*" -and
        $_.FullName -notlike "*\packages\*"
    }
    
    foreach ($file in $files) {
        $totalFiles++
        if (Remove-ChineseFromFile $file.FullName) {
            $modifiedFiles++
        }
    }
}

Write-Host "`nChinese text removal completed!" -ForegroundColor Green
Write-Host "Total files processed: $totalFiles" -ForegroundColor Cyan
Write-Host "Files modified: $modifiedFiles" -ForegroundColor Cyan
Write-Host "`nPlease review the changes and commit them if satisfied." -ForegroundColor Yellow

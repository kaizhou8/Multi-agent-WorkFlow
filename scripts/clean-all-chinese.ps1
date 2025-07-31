# Complete Chinese text cleanup script for Multi-Agent project
Write-Host "Starting comprehensive Chinese text cleanup..." -ForegroundColor Green

$projectRoot = "C:\Users\phoen\Documents\wind\Multi-Agent"

# Function to clean all remaining Chinese content
function Clean-AllChinese {
    param([string]$filePath)
    
    $content = Get-Content $filePath -Raw -Encoding UTF8
    $originalContent = $content
    
    # Clean XML comments with Chinese
    $content = $content -replace '<!-- .* / [^>]*? -->', '<!-- $1 -->'
    $content = $content -replace '<!-- ([^/]*?) / [^>]*? -->', '<!-- $1 -->'
    
    # Clean XAML Text attributes with Chinese
    $content = $content -replace 'Text="([^"]*?) / [^"]*?"', 'Text="$1"'
    $content = $content -replace 'Text="[^"]*? / ([^"]*?)"', 'Text="$1"'
    
    # Clean XAML EmptyView attributes
    $content = $content -replace 'EmptyView="([^"]*?) / [^"]*?"', 'EmptyView="$1"'
    $content = $content -replace 'EmptyView="[^"]*? / ([^"]*?)"', 'EmptyView="$1"'
    
    # Clean XAML Placeholder attributes
    $content = $content -replace 'Placeholder="([^"]*?) / [^"]*?"', 'Placeholder="$1"'
    $content = $content -replace 'Placeholder="[^"]*? / ([^"]*?)"', 'Placeholder="$1"'
    
    # Clean C# XML documentation comments
    $content = $content -replace '/// ([^/]*?) / [^\r\n]*', '/// $1'
    $content = $content -replace '/// <summary>([^<]*?) / [^<]*?</summary>', '/// <summary>$1</summary>'
    $content = $content -replace '/// <returns>([^<]*?) / [^<]*?</returns>', '/// <returns>$1</returns>'
    $content = $content -replace '/// <param[^>]*>([^<]*?) / [^<]*?</param>', '/// <param>$1</param>'
    
    # Clean malformed documentation
    $content = $content -replace 'IDID[^\s]*', 'ID'
    
    # Remove any remaining Chinese characters
    $content = $content -replace '[\u4e00-\u9fff]+[^\r\n]*', ''
    
    # Clean up extra whitespace and empty lines
    $content = $content -replace '\s+\r?\n', "`r`n"
    $content = $content -replace '\r?\n\s*\r?\n\s*\r?\n', "`r`n`r`n"
    $content = $content -replace '^\s*\r?\n', ''
    
    if ($content -ne $originalContent) {
        Set-Content $filePath $content -Encoding UTF8
        Write-Host "Cleaned: $filePath" -ForegroundColor Yellow
        return $true
    }
    return $false
}

# Process all relevant files
$filePatterns = @("*.cs", "*.xaml")
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
        if (Clean-AllChinese $file.FullName) {
            $modifiedFiles++
        }
    }
}

Write-Host "`nComprehensive Chinese cleanup completed!" -ForegroundColor Green
Write-Host "Total files processed: $totalFiles" -ForegroundColor Cyan
Write-Host "Files modified: $modifiedFiles" -ForegroundColor Cyan

# Final verification
Write-Host "`nRunning final verification..." -ForegroundColor Yellow
$remainingChinese = Select-String -Path "$projectRoot\src\**\*.cs", "$projectRoot\src\**\*.xaml" -Pattern "[\u4e00-\u9fff]" -AllMatches

if ($remainingChinese) {
    Write-Host "Warning: Some Chinese text may still remain:" -ForegroundColor Red
    $remainingChinese | Select-Object -First 10 | ForEach-Object { 
        Write-Host "  $($_.Filename):$($_.LineNumber) - $($_.Line.Trim())" -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ All Chinese text has been successfully removed!" -ForegroundColor Green
}

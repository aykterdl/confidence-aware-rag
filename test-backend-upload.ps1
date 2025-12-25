# Test backend PDF upload directly
$pdfPath = "C:\Users\erdal\Downloads\test.pdf"  # Change this to a SMALL test PDF

if (-not (Test-Path $pdfPath)) {
    Write-Host "❌ PDF file not found: $pdfPath" -ForegroundColor Red
    Write-Host "Please create a small test PDF (1-2 pages) or change the path" -ForegroundColor Yellow
    exit
}

Write-Host "📄 Testing backend PDF upload..." -ForegroundColor Cyan
Write-Host "File: $pdfPath" -ForegroundColor Gray
Write-Host "Size: $((Get-Item $pdfPath).Length) bytes" -ForegroundColor Gray
Write-Host ""

$form = @{
    file = Get-Item -Path $pdfPath
    title = "Test Document"
}

Write-Host "🔄 Uploading to backend..." -ForegroundColor Yellow
$startTime = Get-Date

try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/api/ingest/pdf" `
        -Method POST `
        -Form $form `
        -TimeoutSec 600

    $duration = ((Get-Date) - $startTime).TotalSeconds
    
    Write-Host "✅ Upload successful! ($([math]::Round($duration, 1))s)" -ForegroundColor Green
    Write-Host ""
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 5
    
} catch {
    $duration = ((Get-Date) - $startTime).TotalSeconds
    Write-Host "❌ Upload failed after $([math]::Round($duration, 1))s" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}


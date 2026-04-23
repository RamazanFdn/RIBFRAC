# ============================================================
#  AI Fracture Detection  -  GitHub Kurulum Scripti
# ============================================================
# Bu script:
#   1. Bozuk .git klasörünü (varsa) siler
#   2. Yeni bir git deposu başlatır
#   3. İlk commit'i atar
#   4. gh CLI ile public GitHub repo'su oluşturur
#   5. main branch'ini push eder
#
# Çalıştırma:
#   PowerShell'i BU klasörde aç ve şunu yaz:
#     powershell -ExecutionPolicy Bypass -File .\setup-github.ps1
# ============================================================

$ErrorActionPreference = 'Stop'

$repoName = "AI-Fracture-Detection"
$repoDescription = "Yapay zeka destekli kirik ve cikik tespit masaustu uygulamasi (WPF .NET 8)"

Write-Host ""
Write-Host "AI Fracture Detection - GitHub kurulumu basliyor..." -ForegroundColor Cyan
Write-Host ""

# 1) Bozuk .git klasorunu temizle (varsa)
if (Test-Path ".git") {
    Write-Host "Mevcut .git klasoru temizleniyor..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force ".git"
}

# 2) Git init
Write-Host "git init..." -ForegroundColor Green
git init -b main

# 3) Ilk commit
Write-Host "Dosyalar ekleniyor..." -ForegroundColor Green
git add .
git commit -m "Initial commit: WPF .NET 8 masaustu uygulamasi iskeleti"

# 4) gh ile repo olustur
Write-Host ""
Write-Host "GitHub'da public repo olusturuluyor: $repoName" -ForegroundColor Green
gh repo create $repoName --public --description $repoDescription --source=. --remote=origin

# 5) Push
Write-Host ""
Write-Host "Push yapiliyor..." -ForegroundColor Green
git push -u origin main

Write-Host ""
Write-Host "Tamam! Repo adresi:" -ForegroundColor Cyan
gh repo view --web --json url -q .url
Write-Host ""

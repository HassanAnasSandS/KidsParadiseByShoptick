# Kids Paradise — one-click deploy to IIS Live folder
$ErrorActionPreference = "Stop"
$root = "E:\KidsParadiseByShoptickSolution"

Write-Host "==> Building React..."
Set-Location "$root\kidsparadisebyshoptick.react"
npm run build

Write-Host "==> Publishing API to Live..."
Set-Location "$root\KidsParadiseByShoptick.APIs"
dotnet publish -c Release -o "$root\KidsParadiseByShoptick.Published\Live"

Write-Host "==> Ensuring upload folders..."
@("categories", "toys", "reviews", "site") | ForEach-Object {
    New-Item -ItemType Directory -Force -Path "$root\KidsParadiseByShoptick.Published\uploads\$_" | Out-Null
}

Write-Host "==> Triggering app recycle (web.config touch, no BOM)..."
$wc = "$root\KidsParadiseByShoptick.Published\Live\web.config"
$src = Get-Content "$root\KidsParadiseByShoptick.APIs\web.config" -Raw
$src = $src.TrimEnd() + "`r`n<!-- deploy:$(Get-Date -Format o) -->`r`n"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($wc, $src, $utf8NoBom)

# Keep TikTok URL verification files available without Cloudflare/DNS
$tikTokCode = "sMNBLtnQopDSTvuIg1d4DGa3Hq97e9Sb"
$tikTokFileName = "tiktoksMNBLtnQopDSTvuIg1d4DGa3Hq97e9Sb.txt"
[System.IO.File]::WriteAllText("$root\KidsParadiseByShoptick.Published\tiktok-developers-site-verification.txt", $tikTokCode, $utf8NoBom)
[System.IO.File]::WriteAllText("$root\KidsParadiseByShoptick.Published\$tikTokFileName", $tikTokCode, $utf8NoBom)
$www = "$root\KidsParadiseByShoptick.Published\Live\wwwroot"
if (Test-Path $www) {
    [System.IO.File]::WriteAllText("$www\tiktok-developers-site-verification.txt", $tikTokCode, $utf8NoBom)
    [System.IO.File]::WriteAllText("$www\$tikTokFileName", $tikTokCode, $utf8NoBom)
}

# Preserve local secrets if present (publish may not copy gitignored secrets)
$secretsSrc = "$root\KidsParadiseByShoptick.APIs\appsettings.Secrets.json"
$secretsDst = "$root\KidsParadiseByShoptick.Published\Live\appsettings.Secrets.json"
if (Test-Path $secretsSrc) {
    Copy-Item $secretsSrc $secretsDst -Force
    Write-Host "==> Copied appsettings.Secrets.json to Live"
}

Write-Host "Done. Live: $root\KidsParadiseByShoptick.Published\Live"
Write-Host "Site: https://kidsparadise.shoptick.shop"
